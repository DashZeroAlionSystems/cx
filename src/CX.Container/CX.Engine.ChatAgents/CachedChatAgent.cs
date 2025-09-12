using System.Text.Json;
using CX.Engine.ChatAgents.OpenAI;
using CX.Engine.Common.Stores.Json;
using CX.Engine.Common.Tracing;
using CX.Engine.TextProcessors.Splitters;
using JetBrains.Annotations;

namespace CX.Engine.ChatAgents;

public class CachedChatAgent : IChatAgent<OpenAIChatRequest, OpenAIChatResponse>
{
    private readonly Crc32JsonStore _jsonStore;
    private readonly Crc32JsonStore.StoreIdentifier _storeId;
    private readonly OpenAIChatAgent _chatAgent;
    private readonly Func<ChatRequestBase, CXTrace> _getTrace;

    public CachedChatAgent([NotNull] Crc32JsonStore jsonStore, Crc32JsonStore.StoreIdentifier storeId, [NotNull] OpenAIChatAgent chatAgent, Func<ChatRequestBase, CXTrace> getTrace = null)
    {
        _getTrace = getTrace;
        _chatAgent = chatAgent ?? throw new ArgumentNullException(nameof(chatAgent));
        _jsonStore = jsonStore ?? throw new ArgumentNullException(nameof(jsonStore));
        _jsonStore.ValidateResolveClient(ref storeId);
        _storeId = storeId;
    }

    public string Model => _chatAgent.Model;

    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public class CacheValue
    {
        public string Answer { get; set; }

        public CacheValue(string answer)
        {
            Answer = answer;
        }

        public override string ToString() => Answer;
        
        public static implicit operator CacheValue(string answer) => new(answer);
    }

    public async Task<T> RequestAsync<T>(ChatRequestBase ctx)
    {
        async Task<T> DoWorkAsync(TraceOrSpan section)
        {
            if (ctx is not OpenAIChatRequest req)
                throw new InvalidOperationException("ctx has to be OpenAIChatRequest");

            var cacheKey = req.GetCacheKey();

            var cachedResponse = await _jsonStore.GetAsync<CacheValue>(_storeId, cacheKey);

            if (cachedResponse != null)
            {
                section.Output = new
                {
                    FromCache = true,
                    Answer = cachedResponse.Answer
                };
                return JsonSerializer.Deserialize<T>(cachedResponse.Answer);
            }

            var rawRes = await _chatAgent.RequestAsync(ctx);
            var objRes = JsonSerializer.Deserialize<T>(rawRes.Answer);

            await _jsonStore.SetAsync(_storeId, cacheKey, new CacheValue(rawRes.Answer));
            
            section.Output = new
            {
                FromCache = false,
                Answer = rawRes.Answer
            };

            return objRes;
        }

        if (_getTrace != null)
            return await (_getTrace?.Invoke(ctx) ?? CXTrace.Current).ExecuteAsync(trace => DoWorkAsync(trace));
        else
            return await CXTrace.Current.SpanFor("cached-chat-agent", new {
                Prompt = ctx.SystemPrompt,
                Question = ctx.Question
            }).ExecuteAsync(async span => await DoWorkAsync(span));
    }   

    public async Task<ChatResponseBase> RequestAsync(ChatRequestBase ctx)
    {
        if (ctx is not OpenAIChatRequest req)
            throw new InvalidOperationException("ctx has to be OpenAIChatRequest");

        async Task<ChatResponseBase> DoWorkAsync(TraceOrSpan section)
        {
            var cacheKey = req.GetCacheKey();
        
            var cachedResponse = await _jsonStore.GetAsync<CacheValue>(_storeId, cacheKey);

            if (cachedResponse != null)
            {
                section.Output = new
                {
                    FromCache = true,
                    Answer = cachedResponse.Answer
                };
                return new OpenAIChatResponse() { Answer = cachedResponse.Answer };
            }

            ChatResponseBase res = null;
            res = await _chatAgent.RequestAsync(ctx);
            
            await _jsonStore.SetAsync(_storeId, cacheKey, new CacheValue(res!.Answer));

            section.Output = new
            {
                FromCache = false,
                Answer = res.Answer
            };

            return res;
        }

        if (_getTrace != null)
            return await _getTrace.Invoke(req).ExecuteAsync(trace => DoWorkAsync(trace));
        else
            return await CXTrace.Current.SpanFor("cached-chat-agent", new {
                Prompt = req.SystemPrompt,
                Question = req.Question
            }).ExecuteAsync(async span => await DoWorkAsync(span));
    }

    public ChatRequestBase GetRequest(string question = null, List<TextChunk> chunks = null, string systemPrompt = null)
    {
        return _chatAgent.GetRequest(question, chunks, systemPrompt);
    }

    public ChatRequestBase GetRequest<T>(string question = null, List<TextChunk> chunks = null, string systemPrompt = null)
    {
        return _chatAgent.GetRequest<T>(question, chunks, systemPrompt);
    }

    public SchemaBase GetSchema(string name)
    {
        return _chatAgent.GetSchema(name);
    }
}
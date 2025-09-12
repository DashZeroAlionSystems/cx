using System.Text.Json;
using CX.Engine.ChatAgents.Gemini.Schemas;
using CX.Engine.ChatAgents.OpenAI;
using CX.Engine.Common;
using CX.Engine.Common.Json;
using CX.Engine.Common.Tracing;
using CX.Engine.TextProcessors.Splitters;
using Flurl.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;

namespace CX.Engine.ChatAgents.Gemini;

public class GeminiChatAgent : IChatAgent<GeminiChatRequest, GeminiChatResponse>
{
    private GeminiChatAgentOptions _options;
    private SemaphoreSlim _maxConcurrencyLock;
    private readonly ILogger _logger;
    private readonly IDisposable _optionsChangeDisposable;

    public GeminiChatAgent(IOptionsMonitor<GeminiChatAgentOptions> agentOptions, ILogger logger, IServiceProvider sp)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _optionsChangeDisposable = agentOptions.Snapshot(() => Options, o => Options = o, logger, sp);
    }

    public GeminiChatAgentOptions Options
    {
        get => _options;
        private set
        {
            if (_options == value)
                return;

            _options = value;
            _maxConcurrencyLock = new(Options.MaxConcurrentCalls, Options.MaxConcurrentCalls);
        }
    }
    
    public async Task<T> GetResponseAsync<T>(GeminiChatRequest ctx, bool assignResponseFormatIfNull = true)
    {
        if (ctx.ResponseFormat == null && assignResponseFormatIfNull)
            ctx.ResponseFormat = new GeminiSchema<T>();

        var res = await RequestAsync(ctx);

        if (res.IsRefusal)
            throw new GeminiRefusalException(res.Answer!);

        if (res.Answer == null)
            throw new InvalidOperationException("OpenAI response was null.");

        var answer = JsonSerializer.Deserialize<T>(res.Answer);

        if (answer == null)
            throw new InvalidOperationException("OpenAI response could not be deserialized.");

        return answer;
    }

    public string Model => Options.Model;

    public async Task<ChatResponseBase> RequestAsync(ChatRequestBase rawCtx)
    {
        if (rawCtx is not GeminiChatRequest ctx)
            throw new ArgumentException($"Context must be of type {nameof(GeminiChatRequest)}.", nameof(ctx));
        
        var temp = _options.DefaultTemperature;
        
        return await CXTrace.Current.GenerationFor(CXTrace.Section_GenCompletion,
            Options.Model,
            new()
            {
                ["Temperature"] = temp,
                //["Seed"] = 1579
            },
            new
            {
                SystemPrompt = ctx.SystemPrompt,
                History = ctx.History,
                Question = ctx.Question,
                StringContext = ctx.StringContext,
                ResponseFormat = ctx.ResponseFormat?.ToString(),
                MaxCompletionTokens = ctx.MaxCompletionTokens,
                PredictedOutput = ctx.PredictedOutput
            }
        ).ExecuteAsync(async gen =>
        {
            var timeout = ctx.TimeOut ?? TimeSpan.FromMinutes(10);
            var maxRetries = ctx.MaxRetries ?? int.MaxValue;
            var minDelay = ctx.MinDelay ?? TimeSpan.FromSeconds(1);
            var maxDelay = ctx.MaxDelay ?? TimeSpan.FromSeconds(128);

            var rpb = new ResiliencePipelineBuilder();
            rpb.AddRetry(new()
            {
                ShouldHandle = args => ValueTask.FromResult(args.Outcome.Exception is FlurlHttpException
                {
                    StatusCode: 408 or 429 or >= 500 and < 600
                } or FlurlHttpException { InnerException: HttpRequestException } or FlurlHttpTimeoutException),
                BackoffType = DelayBackoffType.Exponential,
                //1 2 4 8 16 32 64 128
                Delay = minDelay,
                MaxDelay = maxDelay,
                MaxRetryAttempts = maxRetries,
                OnRetry = args =>
                {
                    CXTrace.Current.Event(args.Outcome.Exception);
                    _logger.LogWarning(
                        $"{args.Outcome.Exception?.GetType().Name} {args.Outcome.Exception?.Message} encountered. Retrying in {args.RetryDelay}.");
                    return ValueTask.CompletedTask;
                }
            });
            var resiliencePipeline = rpb.Build();

            var req = new GeminiChatHttpRequest
            {
                Model = Options.Model,
                Temperature = temp,
                ResponseFormat = ctx.ResponseFormat,
                PredictedOutput = ctx.PredictedOutput,
                MaxCompletionTokens = ctx.MaxCompletionTokens
            };

            var res = new GeminiChatResponse();

            res.SystemPrompt = ctx.SystemPrompt;
            var sysRole = _options.OnlyUserRole ? "user" : "model";

            if (!string.IsNullOrWhiteSpace(res.SystemPrompt))
                req.Messages.Add(new(sysRole, res.SystemPrompt));
            //Causes hallucinations
            //req.Tools = ctx.Tools;

            foreach (var s in ctx.StringContext)
                req.Messages.Add(new(sysRole, s));

            foreach (var msg in ctx.History)
                if (_options.OnlyUserRole)
                    req.Messages.Add(new("user", msg.Content));
                else
                    req.Messages.Add(new(msg.Role, msg.Content));

            if(!string.IsNullOrWhiteSpace(ctx.Question) || !string.IsNullOrWhiteSpace(ctx.ImageUrl))
                req.Messages.Add(new("user", ctx.Question ?? "", imageUrl: ctx.ImageUrl));

            await CXTrace.Current.SpanFor(CXTrace.Section_Queue, null)
                .ExecuteAsync(async _ => { await _maxConcurrencyLock.WaitAsync(); });

            byte[] response;
            try
            {
                var httpContent = req.GetHttpContent();
                response = await resiliencePipeline.ExecuteAsync(async _ =>
                {
                    var httpRes = await $"https://generativelanguage.googleapis.com/v1beta/models/{_options.Model}:generateContent?key={_options.APIKey}"
                        .AllowHttpStatus(400)
                        .WithTimeout(timeout)
                        .PostAsync(httpContent, HttpCompletionOption.ResponseHeadersRead);

                    if (httpRes.StatusCode == 400)
                    {
                        var msg = await httpRes.GetStringAsync();
                        throw new OpenAIException(
                            $"OpenAI Completions API returned status {httpRes.StatusCode} with message: {msg}");
                    }

                    var valid = httpRes.StatusCode is >= 200 and < 300;

                    if (!valid)
                    {
                        var msg = await httpRes.GetStringAsync();
                        throw new InvalidOperationException(
                            $"OpenAI Completions API returned {httpRes.StatusCode} with message: {msg}");
                    }

                    return await httpRes.GetBytesAsync();
                });
            }
            finally
            {
                _maxConcurrencyLock.Release();
            }
            
            res.PopulateFromBytes(response);

            gen.Output = new { Answer = res.Answer };
            gen.CompletionTokens = res.ChatUsage?.CompletionTokens;
            gen.CachedTokens = res.ChatUsage?.PromptTokensDetails?.CachedTokens;
            gen.RawTotalTokens = res.ChatUsage?.TotalTokens;
            gen.RawPromptTokens = res.ChatUsage?.PromptTokens;
            gen.TotalTokens = res.ChatUsage?.TotalTokens;
            gen.PromptTokens = res.ChatUsage?.PromptTokens;
            gen.ReasoningTokens = res.ChatUsage?.CompletionTokensDetails?.ReasoningTokens;
            gen.AudioTokens = res.ChatUsage?.CompletionTokensDetails?.AudioTokens;
            gen.AcceptedPredictionTokens = res.ChatUsage?.CompletionTokensDetails?.AcceptedPredictionTokens;
            gen.RejectedPredictionTokens = res.ChatUsage?.CompletionTokensDetails?.RejectedPredictionTokens;

            return res;
        });
    }

    public GeminiChatRequest GetRequestContext(string question = null, List<TextChunk> chunks = null, string systemPrompt = null) => new(question, chunks, systemPrompt);
    ChatRequestBase IChatAgent.GetRequest(string question, List<TextChunk> chunks, string systemPrompt) => GetRequestContext(question, chunks, systemPrompt);
    
    public ChatRequestBase GetRequest<T>(string question = null, List<TextChunk> chunks = null, string systemPrompt = null)
    {
        throw new NotSupportedException();
    }

    public SchemaBase GetSchema(string name) => new GeminiSchema(name);
}
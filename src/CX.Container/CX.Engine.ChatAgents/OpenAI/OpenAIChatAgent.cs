using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using CX.Engine.ChatAgents.OpenAI.Schemas;
using CX.Engine.Common;
using CX.Engine.Common.Json;
using CX.Engine.Common.Stores.Json;
using CX.Engine.Common.Tracing;
using CX.Engine.TextProcessors.Splitters;
using Flurl.Http;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;

namespace CX.Engine.ChatAgents.OpenAI;

public class OpenAIChatAgent : IChatAgent<OpenAIChatRequest, OpenAIChatResponse>, IDisposable
{
    public volatile Task GlobalBackOff = Task.CompletedTask;

    public readonly OpenAIMetrics Metrics;

    public static class Models
    {
        public const string gpt_4o_mini = "gpt-4o-mini";
        public const string gpt_4o = "gpt-4o";
        public const string gpt_3_5_turbo = "gpt-3.5-turbo";
        public const string o1_preview = "o1-preview";
        public const string o1_mini = "o1-mini";
        public const string o1 = "o1";
    }

    public static JsonObject WrapSchema(JsonObject jsonSchema, [NotNull] string openAIName)
    {
        if (openAIName == null) throw new ArgumentNullException(nameof(openAIName));

        var jdoc = new JsonObject();
        jdoc["name"] = openAIName;
        jdoc["schema"] = jsonSchema;
        jdoc["strict"] = true;
        return jdoc;
    }

    private OpenAIChatAgentOptions _options;

    public OpenAIChatAgentOptions Options
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

    private SemaphoreSlim _maxConcurrencyLock;
    private readonly ILogger _logger;
    private readonly Crc32JsonStore _jsonStore;
    private readonly IDisposable _optionsChangeDisposable;

    public OpenAIChatAgent(MonitoredOptionsSection<OpenAIChatAgentOptions> agentOptions, ILogger logger, IServiceProvider sp, string name, [NotNull] Crc32JsonStore jsonStore)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _jsonStore = jsonStore ?? throw new ArgumentNullException(nameof(jsonStore));
        _optionsChangeDisposable = agentOptions.Monitor.Snapshot(() => Options, o => Options = o, logger, sp, agentOptions.Section);
        Metrics = new(sp, name);
    }

    public Task<T> GetResponseAsync<T>(OpenAIChatRequest<T> ctx) => GetResponseAsync<T>((OpenAIChatRequest)ctx);

    public async Task<T> GetResponseAsync<T>(OpenAIChatRequest ctx, bool assignResponseFormatIfNull = true)
    {
        if (ctx.ResponseFormat == null && assignResponseFormatIfNull)
            ctx.ResponseFormat = new OpenAISchema<T>();

        var res = await RequestAsync(ctx);

        if (res.IsRefusal)
            throw new OpenAIRefusalException(res.Answer!);

        if (res.Answer == null)
            throw new InvalidOperationException("OpenAI response was null.");

        var answer = JsonSerializer.Deserialize<T>(res.Answer);

        if (answer == null)
            throw new InvalidOperationException("OpenAI response could not be deserialized.");

        return answer;
    }

    public string Model => Options.Model;

    public async Task<ChatResponseBase> RequestAsync(ChatRequestBase rawReq)
    {
        if (rawReq is not OpenAIChatRequest request)
            throw new ArgumentException($"Request must be of type {nameof(OpenAIChatRequest)}.", nameof(request));
        
        var opts = _options;

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
                SystemPrompt = request.SystemPrompt,
                InputAttachments = request.Attachments,
                History = request.History,
                Question = request.Question,
                StringContext = request.StringContext,
                ResponseFormat = request.ResponseFormat?.ToString(),
                MaxCompletionTokens = request.MaxCompletionTokens,
                PredictedOutput = request.PredictedOutput
            }
        ).ExecuteAsync(async gen =>
        {
            if (opts.EnableCaching)
            {
                ChatResponseBase cachedValue = null;
                await CXTrace.Current.SpanFor("check-cache", new { }).ExecuteAsync(async _ =>
                {
                    var cacheKey = request.GetCacheKey();
                    cachedValue = await _jsonStore.GetAsync<OpenAIChatResponse>(opts.StoreId, cacheKey);
                });

                if (cachedValue != null)
                {
                    gen.Output = new { FromCache = true };
                    return cachedValue;
                }
            }

            var timeout = request.TimeOut ?? TimeSpan.FromMinutes(10);
            var maxRetries = request.MaxRetries ?? int.MaxValue;
            var minDelay = request.MinDelay ?? TimeSpan.FromSeconds(1);
            var maxDelay = request.MaxDelay ?? TimeSpan.FromSeconds(128);
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
                    var ex = args.Outcome.Exception;
                    if (ex != null)
                    {
                        if (args.Outcome.Exception is FlurlHttpException { StatusCode: 429 })
                        {
                            if (_options.BackOffTimeOut > TimeSpan.Zero)
                                GlobalBackOff = Task.Delay(_options.BackOffTimeOut);
                            Metrics.Asks_429ExceptionsCount.Inc();
                        }

                        if (args.Outcome.Exception is TimeoutException or FlurlHttpTimeoutException
                            or TaskCanceledException)
                            Metrics.Asks_TimeOutExceptionsCount.Inc();

                        Metrics.Asks_ExceptionsCount.Inc();
                        CXTrace.Current.Event(ex);
                        _logger.LogWarning($"{ex.GetType().Name} {ex.Message} encountered for '{request.Question?.Replace("'", "&#39;")}' with system prompt {request.SystemPrompt}. Retrying in {args.RetryDelay}.");
                    }

                    return ValueTask.CompletedTask;
                }
            });
            var resiliencePipeline = rpb.Build();
            var req = new ChatRequest
            {
                Model = Options.Model,
                Temperature = temp,
                ResponseFormat = request.ResponseFormat,
                PredictedOutput = request.PredictedOutput,
                MaxCompletionTokens = request.MaxCompletionTokens
            };

            var res = new OpenAIChatResponse();

            res.SystemPrompt = request.SystemPrompt;
            var sysRole = _options.OnlyUserRole ? "user" : "system";

            if (!string.IsNullOrWhiteSpace(res.SystemPrompt))
                req.Messages.Add(new(sysRole, res.SystemPrompt));
            //Causes hallucinations
            //req.Tools = ctx.Tools;
            if(request.Chunks is not null)
                foreach (var chunk in request.Chunks)
                {
                    var sb = new StringBuilder();
                    sb.Append(chunk.GetContextString());

                    if (request.UseAttachments)
                    {
                        var atts = chunk.Metadata.GetAttachments(false);
                        if (atts != null)
                        {
                            sb.Append(
                                "\n\n\nInclude a markdown referency-style link of the format [filename](url) to one of the documents below if related to the user's question, and explain why each one is linked.  Do not include any other links.  ");
                            foreach (var att in atts)
                                sb.Append("\n- " + att.AsMarkdownLink());
                        }
                    }

                    req.Messages.Add(new(sysRole, sb.ToString()));
                }

            foreach (var s in request.StringContext)
                req.Messages.Add(new(sysRole, s));

            foreach (var msg in request.History)
                if (_options.OnlyUserRole)
                    req.Messages.Add(new("user", msg.Content));
                else
                    req.Messages.Add(new(msg.Role, msg.Content));
            if(!string.IsNullOrWhiteSpace(request.Question) || !string.IsNullOrWhiteSpace(request.ImageUrl))
                req.Messages.Add(new("user", request.Question ?? "", imageUrl: request.ImageUrl));

            await CXTrace.Current.SpanFor(CXTrace.Section_Queue, null)
                .ExecuteAsync(async _ => { await _maxConcurrencyLock.WaitAsync(); });

            IFlurlResponse responseWithHeaders = null;
            Stopwatch stopWatch;
            byte[] response;
            try
            {
                var httpContent = req.GetHttpContent();
                stopWatch = Stopwatch.StartNew();
                response = await resiliencePipeline.ExecuteAsync(async _ =>
                {
                    if (!GlobalBackOff.IsCompleted)
                    {
                        await CXTrace.Current.SpanFor("global-back-off", null)
                            .ExecuteAsync(async _ =>
                            {
                                while (!GlobalBackOff.IsCompleted)
                                    await GlobalBackOff;
                            });
                    }

                    var httpRes = await opts.BaseUrl
                        .WithHeaders(new
                        {
                            Authorization = $"Bearer {Options.APIKey}"
                        })
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

                    responseWithHeaders = httpRes;

                    return await httpRes.GetBytesAsync();
                });
                stopWatch.Stop();
            }
            finally
            {
                _maxConcurrencyLock.Release();
            }
            
            res.InputAttachments = request.Attachments;
            res.PopulateFromBytes(response);
            
            res.ResponseTime = stopWatch.Elapsed;

            gen.Output = new { Answer = res.Answer, ToolCalls = res.ToolCalls };
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

            {
                if (responseWithHeaders != null && responseWithHeaders.Headers.TryGetFirst("x-ratelimit-limit-requests", out var xs) && int.TryParse(xs, out var xi))
                    gen.XRateLimitLimitRequests = xi;
            }
            {
                if (responseWithHeaders != null && responseWithHeaders.Headers.TryGetFirst("x-ratelimit-limit-tokens", out var xs) && int.TryParse(xs, out var xi))
                    gen.XRateLimitLimitTokens = xi;
            }
            {
                if (responseWithHeaders != null && responseWithHeaders.Headers.TryGetFirst("x-ratelimit-remaining-requests", out var xs) && int.TryParse(xs, out var xi))
                    gen.XRateLimitRemainingRequests = xi;
            }
            {
                if (responseWithHeaders != null && responseWithHeaders.Headers.TryGetFirst("x-ratelimit-remaining-tokens", out var xs) && int.TryParse(xs, out var xi))
                    gen.XRateLimitRemainingTokens = xi;
            }
            {
                if (responseWithHeaders != null && responseWithHeaders.Headers.TryGetFirst("x-ratelimit-reset-requests", out var xs))
                    gen.XRateLimitResetRequests = xs;
            }
            {
                if (responseWithHeaders != null && responseWithHeaders.Headers.TryGetFirst("x-ratelimit-reset-tokens", out var xs))
                    gen.XRateLimitResetTokens = xs;
            }

            Metrics.Asks_TotalTokenCount.Inc(res.ChatUsage?.TotalTokens ?? 0);

            if (_options.ApplyCachedTokenDiscountToInputTokens && gen.CachedTokens > 0)
            {
                gen.TotalTokens -= gen.CachedTokens / 2;
                gen.PromptTokens -= gen.CachedTokens / 2;
            }
            
            if (opts.EnableCaching)
                await CXTrace.Current.SpanFor("update-cache", new { }).ExecuteAsync(async _ =>
                {
                    var cacheKey = request.GetCacheKey();
                    await _jsonStore.SetAsync(opts.StoreId, cacheKey, res);
                });

            return res;
        });
    }

    /// <summary>
    /// Prepopulates the agent field with Options.Model
    /// </summary>
    public OpenAIChatRequest GetRequest(string question = null, List<TextChunk> chunks = null, string systemPrompt = null) => new(this, question, chunks, systemPrompt);
    ChatRequestBase IChatAgent.GetRequest(string question, List<TextChunk> chunks, string systemPrompt) => GetRequest(question, chunks, systemPrompt);
    public OpenAIChatRequest<T> GetRequest<T>(string question = null, List<TextChunk> chunks = null, string systemPrompt = null) => new(question, this, chunks, systemPrompt);
    ChatRequestBase IChatAgent.GetRequest<T>(string question, List<TextChunk> chunks, string systemPrompt) => GetRequest<T>(question, chunks, systemPrompt);

    public SchemaBase GetSchema(string name) => new OpenAISchema(name);

    public void Dispose()
    {
        _optionsChangeDisposable?.Dispose();
    }
}
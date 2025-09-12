using System.Text;
using System.Threading.RateLimiting;
using CX.Engine.Archives;
using CX.Engine.Common;
using CX.Engine.Common.Tracing;
using CX.Engine.Common.Tracing.Langfuse;
using Flurl;
using Flurl.Http;
using IdentityModel.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.RateLimiting;

namespace CX.Engine.Assistants.VectorMind;

public class VectormindLiveAssistant : IAssistant, IDisposable
{
    private VectormindLiveAssistantOptions _options;
    // ReSharper disable once NotAccessedField.Local
    private readonly ILogger _logger;
    private OAuthTokenResponse _tokenResponse;
    private readonly SemaphoreSlim _accessTokenlock = new(1, 1);
    private readonly ResiliencePipeline _askResiliencePipeline;
    private readonly ResiliencePipeline _accessTokenResiliencePipeline;
    private readonly LangfuseService _langfuseService;
    private readonly DynamicSlimLock _maxAskConcurrencyLock = new(1);
    private volatile int _maxAskConcurrencyItemsInQueue;
    private readonly IDisposable _optionsChangeDisposable;
    public string SystemPrompt { get; set; }
    public string ContextualizePrompt { get; set; }
    
    public VectormindLiveAssistant(IOptionsMonitor<VectormindLiveAssistantOptions> options, ILogger logger,
        LangfuseService langfuseService, IServiceProvider sp)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _langfuseService = langfuseService ?? throw new ArgumentNullException(nameof(langfuseService));
        
        _optionsChangeDisposable = options.Snapshot(() => _options, o =>
            {
                _options = o;
                _maxAskConcurrencyLock.SetMaxCount(_options.MaxConcurrentAsks);
            }, _logger, sp);
        
        var opts = _options;
        
        var rpb = new ResiliencePipelineBuilder();
        rpb.AddRetry(new()
            {
                ShouldHandle = args => ValueTask.FromResult(args.Outcome.Exception is FlurlHttpException
                    {
                        StatusCode: 400 or 401 or 403 or 408 or 429 or >= 500 and < 600
                    } or
                    FlurlHttpException { InnerException: HttpRequestException }),
                BackoffType = DelayBackoffType.Exponential,
                //1 2 4 8 16 30
                Delay = TimeSpan.FromMilliseconds(opts.RetryDelayMs),
                MaxDelay = TimeSpan.FromSeconds(60),
                OnRetry = args =>
                {
                    CXTrace.Current.Event(args.Outcome.Exception);
                    logger.LogWarning(
                        $"{args.Outcome.Exception?.GetType().Name} {args.Outcome.Exception?.Message} encountered. Retrying in {args.RetryDelay}.");
                    return ValueTask.CompletedTask;
                }
            })
            .AddTimeout(TimeSpan.FromMinutes(10));
        _askResiliencePipeline = rpb.Build();

        rpb = new();
        rpb.AddRetry(new()
            {
                ShouldHandle = args => ValueTask.FromResult(args.Outcome.Exception is FlurlHttpException
                {
                    StatusCode: 400 or 408 or 429 or >= 500 and < 600
                } or FlurlHttpException { InnerException: HttpRequestException }),
                BackoffType = DelayBackoffType.Exponential,
                //1 2 4 8 16 30
                Delay = TimeSpan.FromSeconds(1),
                MaxDelay = TimeSpan.FromSeconds(60),
                OnRetry = args =>
                {
                    CXTrace.Current.Event(args.Outcome.Exception);
                    logger.LogWarning(
                        $"{args.Outcome.Exception?.GetType().Name} {args.Outcome.Exception?.Message} encountered. Retrying in {args.RetryDelay}.");
                    return ValueTask.CompletedTask;
                }
            })
            .AddRateLimiter(new RateLimiterStrategyOptions
            {
                DefaultRateLimiterOptions = new()
                {
                    PermitLimit = 1,
                    QueueLimit = 1,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                }
            })
            .AddTimeout(TimeSpan.FromMinutes(10));
        _accessTokenResiliencePipeline = rpb.Build();
    }

    public async Task<string> GetCachedAccessTokenAsync()
    {
        return await CXTrace.Current.SpanFor(CXTrace.Section_GetAccessToken, null)
            .ExecuteAsync(async span =>
            {
                var token = _tokenResponse;
                if (token == null || token.ExpiresAt < DateTime.UtcNow.AddMinutes(-15))
                {
                    await _accessTokenlock.WaitAsync();
                    try
                    {
                        if (_tokenResponse != null && _tokenResponse.ExpiresAt >= DateTime.UtcNow.AddMinutes(-15))
                        {
                            span.Output = "Successfully shared new token";
                            return _tokenResponse.AccessToken;
                        }

                        _tokenResponse = null;
                        var res = await GetAccessTokenAsync();
                        span.Output = "Successfully acquired access token";
                        return res;
                    }
                    finally
                    {
                        _accessTokenlock.Release();
                    }
                }

                span.Output = "Successful retrieved from cache";
                return token.AccessToken;
            });
    }
    
    public async Task<string> GetAccessTokenAsync()
    {
        var opts = _options;
        
        return await CXTrace.Current.SpanFor(CXTrace.Section_CallAPI)
            .ExecuteAsync(async span =>
            {
                var response = await _accessTokenResiliencePipeline.ExecuteAsync(async token =>
                {
                    var client = new FlurlClient();
                    var disco = await client.HttpClient.GetDiscoveryDocumentAsync(opts.TokenUrl, token);
                    if (disco.IsError) throw new HttpRequestException(disco.Error);
                    // Request token
                    var tokenResponse = await client.HttpClient.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
                    {
                        Address = disco.TokenEndpoint,
                        ClientId = opts.ClientId,
                        ClientSecret = opts.ClientSecret,
                        Scope = "api"
                    }, token);
                    if (tokenResponse.IsError) throw new HttpRequestException(tokenResponse.Error);
                    return new OAuthTokenResponse()
                    {
                        AccessToken = tokenResponse.AccessToken,
                        TokenType = tokenResponse.TokenType,
                        ExpiresIn = tokenResponse.ExpiresIn
                    };
                });

                _tokenResponse = response;
                _tokenResponse.ExpiresAt = DateTime.UtcNow.AddSeconds(response.ExpiresIn);
                span.Output = new
                {
                    response.ExpiresIn,
                    _tokenResponse.ExpiresAt
                };
                return response.AccessToken;
            });
    }

    public IChunkArchive ChunkArchive => null;

    public async Task<AssistantAnswer> AskStructuredAsync(string question, AgentRequest ctx)
    {
        var opts = _options;
        if (opts.Structured == null)
            throw new InvalidOperationException("Structured data is not defined");

        opts.Structured.Validate();
            
        var trace = new CXTrace(_langfuseService, ctx.UserId, ctx.SessionId)
            .WithName((ctx.UserId + ": " + question).Preview(50))
            .WithTags("vectormind-live", "ask");

        return await trace
            .WithInput(new
            {
                Question = question
            })
            .ExecuteAsync(async _ => {
                var response = await trace.SpanFor(CXTrace.Section_CallAPI, new { Question = question }).ExecuteAsync(
                    async span =>
                    {
                        //https://playground.vectormind.chat/_apis/server/api/StructuredData?QuestionText=I want fuel efficient cars&ChannelName=weelee-api-v3
                        
                        var itemsInQueue = Interlocked.Increment(ref _maxAskConcurrencyItemsInQueue);
                        await CXTrace.Current.SpanFor(CXTrace.Section_Queue,
                            new
                            {
                                ItemsInQueue = itemsInQueue
                            }).ExecuteAsync(async span =>
                        {
                            await _maxAskConcurrencyLock.WaitAsync();
                            Interlocked.Decrement(ref _maxAskConcurrencyItemsInQueue);
                            span.Output = "Left queue";
                        });
                        
                        var response = await _askResiliencePipeline.ExecuteAsync(async _ => await opts.APIBaseUrl
                            .AppendPathSegment("/api/StructuredData")
                            .AppendQueryParam("QuestionText", question)
                            .AppendQueryParam("ChannelName", opts.Structured.ChannelName)
                            .WithHeader("x-api-key", opts.Structured.ApiKey)
                            .WithHeader("x-api-secret", opts.Structured.ApiSecret)
                            .GetAsync()
                            .ReceiveString());
                        span.Output = response;
                        return response;
                    });

                trace.Output = response.Preview(5_000);
                return new AssistantAnswer(response);
            });
    }

    public async Task<AssistantAnswer> AskAsync(string question, AgentRequest ctx)
    {
        var opts = _options;

        if (opts.Structured?.Enabled ?? false)
            return await AskStructuredAsync(question, ctx);
 
        try
        {
            
            var trace = new CXTrace(_langfuseService, ctx.UserId, ctx.SessionId)
                .WithName((ctx.UserId + ": " + question).Preview(50))
                .WithTags("vectormind-live", "ask");

            return await trace
                .WithInput(new
                {
                    Question = question
                })
                .ExecuteAsync(async _ =>
                {
                    var accessToken = await GetCachedAccessTokenAsync();

                    var itemsInQueue = Interlocked.Increment(ref _maxAskConcurrencyItemsInQueue);
                    await CXTrace.Current.SpanFor(CXTrace.Section_Queue,
                        new
                        {
                            ItemsInQueue = itemsInQueue
                        }).ExecuteAsync(async span =>
                    {
                        await _maxAskConcurrencyLock.WaitAsync();
                        Interlocked.Decrement(ref _maxAskConcurrencyItemsInQueue);
                        span.Output = "Left queue";
                    });

                    try
                    {
                        var response = await trace.SpanFor(CXTrace.Section_CallAPI, new { Question = question }).ExecuteAsync(
                            async span =>
                            {
                                var response = await _askResiliencePipeline.ExecuteAsync(async _ => await opts.APIBaseUrl
                                    .AppendPathSegment("/api/DBAction")
                                    .WithHeader("Authorization", "Bearer " + accessToken)
                                    .SetQueryParams(new { Question = question, ThreadName = Guid.NewGuid().ToString(), opts.BotId })
                                    .PostAsync()
                                    .ReceiveJson<ApiResponse>());
                                span.Output = response;
                                return response;
                            });

                        var res = new AssistantAnswer();
                        res.Answer = response.Message!;

                        if (response.Citations != null)
                        {
                            res.Attachments ??= new();
                            foreach (var citation in response.Citations)
                            {
                                var att = new AttachmentInfo();
                                att.FileName = citation.Name;
                                att.Description = "";
                                att.FileUrl = citation.Url;

                                att.DoGetContentStreamAsync = async () =>
                                {
                                    var url = (opts.APIBaseUrl + att.FileUrl);
                                    try
                                    {
                                        var httpStream = await url.GetStreamAsync();
                                        var memoryStream = await httpStream.CopyToMemoryStreamAsync();
                                        return memoryStream;
                                    }
                                    catch (Exception ex)
                                    {
                                        _logger.LogError(ex, $"Error downloading attachment with url {url}");
                                        return new MemoryStream(Encoding.UTF8.GetBytes(ex.GetType().Name + ": " + ex.Message));
                                    }
                                };
                                res.Attachments.Add(att);
                            }
                        }

                        trace.Output = res.GetCellContent();
                        return res;
                    }
                    finally
                    {
                        _maxAskConcurrencyLock.Release();
                    }
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"During {nameof(VectormindLiveAssistant)}.{nameof(AskAsync)}");
            return new(ex.GetType().Name + ": " + ex.Message);
        }
    }

    public void Dispose()
    {
        _optionsChangeDisposable?.Dispose();
    }
}
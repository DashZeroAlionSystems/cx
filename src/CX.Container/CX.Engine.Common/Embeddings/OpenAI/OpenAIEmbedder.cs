using System.Threading.RateLimiting;
using CX.Engine.Common.Tracing;
using Flurl.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.RateLimiting;

namespace CX.Engine.Common.Embeddings.OpenAI;

public class OpenAIEmbedder
{
    public static class Models
    {
        public const string text_embedding_ada_002 = "text-embedding-ada-002";
        public const string text_embedding_3_large = "text-embedding-3-large";
        public const string text_embedding_3_small = "text-embedding-3-small";
    }

    public readonly OpenAIEmbedderOptions Options;

    private readonly ResiliencePipeline _resiliencePipeline;

    public OpenAIEmbedder(IOptions<OpenAIEmbedderOptions> options, ILogger<OpenAIEmbedder> logger)
    {
        Options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        Options.Validate();

        var rpb = new ResiliencePipelineBuilder();
        rpb.AddRetry(new()
            {
                ShouldHandle = args => ValueTask.FromResult(
                    args.Outcome.Exception is FlurlHttpException { StatusCode: 408 or 429 or >= 500 and < 600 }
                        or FlurlHttpException { InnerException: HttpRequestException }),
                BackoffType = DelayBackoffType.Exponential,
                //1 2 4 8 16 30
                Delay = TimeSpan.FromSeconds(1),
                MaxDelay = TimeSpan.FromSeconds(30),
                MaxRetryAttempts = int.MaxValue,
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
                    PermitLimit = Options.MaxConcurrentCalls,
                    QueueLimit = 1_000_000,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                }
            })
            .AddTimeout(TimeSpan.FromMinutes(3));
        _resiliencePipeline = rpb.Build();
    }

    public async Task<EmbeddingResponse> GetAsync(string model, string text)
    {
        const string apiUrl = "https://api.openai.com/v1/embeddings";

        var response = await _resiliencePipeline.ExecuteAsync(async _ =>
            await apiUrl
                .WithHeader("Authorization", $"Bearer {Options.APIKey}")
                .PostJsonAsync(new
                {
                    input = text,
                    model
                })
                .ReceiveJson<EmbeddingResponse>()
        );

        if (response.Object != "list")
            throw new InvalidOperationException("Unexpected response object");

        if (response.Data is not { Count: 1 } || response.Data[0] is null)
            throw new InvalidOperationException("Unexpected number of embeddings");

        if (response.Data[0].Object != "embedding")
            throw new InvalidOperationException("Unexpected data object");

        if (response.Data[0].Index != 0)
            throw new InvalidOperationException("Unexpected data index");

        int expectedSize;

        if (model == Models.text_embedding_3_large)
            expectedSize = 3072;
        else if (model is Models.text_embedding_ada_002 or Models.text_embedding_3_small)
            expectedSize = 1536;
        else
            throw new InvalidOperationException($"Unexpected model for embeddings: {model}");

        if (response.Data[0].Embedding.Count != expectedSize)
            throw new InvalidOperationException(
                $"Unexpected embedding length (expected {expectedSize}, found {response.Data[0].Embedding.Count})");

        if (response.Usage.PromptTokens <= 0 || response.Usage.TotalTokens <= 0)
            throw new InvalidOperationException("Unexpected usage info");

        if (response.Usage.PromptTokens != response.Usage.TotalTokens)
            throw new InvalidOperationException(
                $"Prompt tokens ({response.Usage.PromptTokens:#,##0}) do not match total tokens ({response.Usage.TotalTokens:#,##0})");

        return response;
    }

    public static bool IsValidModel(string s) => s is Models.text_embedding_3_large or Models.text_embedding_ada_002 or Models.text_embedding_3_small;

    public static void ThrowIfNotValidModel(string s)
    {
        if (!IsValidModel(s))
            throw new ArgumentException($"Invalid OpenAI embedding model: {s}");
    }
}
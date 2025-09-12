using Azure;
using Azure.AI.ContentSafety;
using CX.Engine.Common.Tracing;
using Microsoft.Extensions.Logging;
using Polly;

namespace CX.Engine.TextProcessors;

public class AzureContentSafety : ITextProcessor
{
    public const string CategoryHate = "Hate";
    public const string CategoryViolence = "Violence";
    public const string CategorySexual = "Sexual";
    public const string CategorySelfHarm = "Self-harm";

    public readonly AzureContentSafetyOptions Options;

    private readonly ResiliencePipeline _resiliencePipeline;

    public AzureContentSafety(AzureContentSafetyOptions options, ILogger logger)
    {
        Options = options ?? throw new ArgumentNullException(nameof(options));

        if (logger == null)
            throw new ArgumentNullException(nameof(logger));

        Options.Validate();

        var rpb = new ResiliencePipelineBuilder();
        rpb.AddRetry(new()
        {
            //Unclear how these exceptions look.  There are at least 429s to deal with, + network issues.
            //Auth failures should not be handled.  We will have make this handler more specific as these occur.
            ShouldHandle = ctx => ValueTask.FromResult(ctx.Outcome.Exception != null),
            BackoffType = DelayBackoffType.Exponential,
            //1 2 4 8 16 30
            Delay = TimeSpan.FromSeconds(1),
            MaxDelay = TimeSpan.FromSeconds(Options.RetryMaxDelaySeconds),
            MaxRetryAttempts = int.MaxValue,
            OnRetry = args =>
            {
                CXTrace.Current.Event(args.Outcome.Exception);
                logger.LogWarning(
                    $"{args.Outcome.Exception?.GetType().Name} {args.Outcome.Exception?.Message} encountered. Retrying in {args.RetryDelay}.");
                return ValueTask.CompletedTask;
            }
        }).AddTimeout(TimeSpan.FromSeconds(Options.RetryTimeoutSeconds));

        _resiliencePipeline = rpb.Build();
    }

    public async Task<string> ProcessAsync(string text)
    {
        return await CXTrace.Current.SpanFor(CXTrace.Section_ContentSafety,
                new
                {
                    Text = text,
                    RetryMaxDelaySeconds = Options.RetryMaxDelaySeconds,
                    RetryTimeoutSeconds = Options.RetryTimeoutSeconds,
                    FailHard = Options.FailHard,
                    ExceptionHateLevel = Options.ExceptionHateLevel,
                    ExceptionSexualLevel = Options.ExceptionSexualLevel,
                    ExceptionViolenceLevel = Options.ExceptionViolenceLevel,
                    ExceptionSelfHarmLevel = Options.ExceptionSelfHarmLevel
                })
            .ExecuteAsync(async span =>
            {
                var contentSafetyClient = new ContentSafetyClient(new(Options.Endpoint), new AzureKeyCredential(Options.ApiKey));

                Response<AnalyzeTextResult> res;

                if (Options.FailHard!.Value)
                {
                    res = await _resiliencePipeline.ExecuteAsync(async _ =>
                        await contentSafetyClient.AnalyzeTextAsync(new AnalyzeTextOptions(text)));
                }
                else
                {
                    try
                    {
                        res = await _resiliencePipeline.ExecuteAsync(async _ =>
                            await contentSafetyClient.AnalyzeTextAsync(new AnalyzeTextOptions(text)));
                    }
                    catch (Exception ex)
                    {
                        CXTrace.Current.Event(ex);
                        span.Output = new { Analyzed = false, Exception = ex.Message };
                        return text;
                    }
                }

                //See code: https://learn.microsoft.com/en-us/dotnet/api/overview/azure/ai.contentsafety-readme?view=azure-dotnet
                //See levels: https://learn.microsoft.com/en-us/azure/ai-services/content-safety/concepts/harm-categories?tabs=definitions#text-content
                var hateLevel = res?.Value.CategoriesAnalysis.FirstOrDefault(a => a.Category == TextCategory.Hate)?.Severity;
                var sexualLevel = res?.Value.CategoriesAnalysis.FirstOrDefault(a => a.Category == TextCategory.Sexual)?.Severity;
                var violenceLevel = res?.Value.CategoriesAnalysis.FirstOrDefault(a => a.Category == TextCategory.Violence)?.Severity;
                var selfHarmLevel = res?.Value.CategoriesAnalysis.FirstOrDefault(a => a.Category == TextCategory.SelfHarm)?.Severity;

                var levels = new
                {
                    HateLevel = hateLevel,
                    SexualLevel = sexualLevel,
                    ViolenceLevel = violenceLevel,
                    SelfHarmLevel = selfHarmLevel
                };

                if (hateLevel >= Options.ExceptionHateLevel)
                {
                    span.Output = new
                    {
                        Analyzed = true,
                        Blocked = true,
                        BlockedFor = CategoryHate,
                        DetectedLevel = hateLevel.Value,
                        ExceptionLevel = Options.ExceptionHateLevel,
                        Levels = levels
                    };
                    throw new ContentSafetyException(CategoryHate, hateLevel.Value);
                }

                if (sexualLevel >= Options.ExceptionSexualLevel)
                {
                    span.Output = new
                    {
                        Analyzed = true,
                        Blocked = true,
                        BlockedFor = CategorySexual,
                        DetectedLevel = sexualLevel.Value,
                        ExceptionLevel = Options.ExceptionSexualLevel,
                        Levels = levels
                    };
                    throw new ContentSafetyException(CategorySexual, sexualLevel.Value);
                }

                if (violenceLevel >= Options.ExceptionViolenceLevel)
                {
                    span.Output = new
                    {
                        Analyzed = true,
                        Blocked = true,
                        BlockedFor = CategoryViolence,
                        DetectedLevel = violenceLevel.Value,
                        ExceptionLevel = Options.ExceptionViolenceLevel,
                        Levels = levels
                    };
                    throw new ContentSafetyException(CategoryViolence, violenceLevel.Value);
                }

                if (selfHarmLevel >= Options.ExceptionSelfHarmLevel)
                {
                    span.Output = new
                    {
                        Analyzed = true,
                        Blocked = true,
                        BlockedFor = CategorySelfHarm,
                        DetectedLevel = selfHarmLevel.Value,
                        ExceptionLevel = Options.ExceptionSelfHarmLevel,
                        Levels = levels
                    };
                    throw new ContentSafetyException(CategorySelfHarm, selfHarmLevel.Value);
                }

                span.Output = new
                {
                    Analyzed = true,
                    Blocked = false,
                    Levels = levels
                };

                return text;
            });
    }
}
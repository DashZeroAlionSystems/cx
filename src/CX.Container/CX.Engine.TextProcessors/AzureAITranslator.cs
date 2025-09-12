using System.Text;
using Azure;
using Azure.AI.Translation.Text;
using CX.Engine.Common.Tracing;
using CX.Engine.TextProcessors.Splitters;
using Microsoft.Extensions.Logging;
using Polly;

namespace CX.Engine.TextProcessors;

public class AzureAITranslator : ITextProcessor
{
    public const int MaxCharactersPerRequest = 50_000;

    private readonly AzureAITranslatorOptions _options;
    private readonly ResiliencePipeline _resiliencePipeline;

    public AzureAITranslator(AzureAITranslatorOptions options, ILogger logger)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _options.Validate();

        var rpb = new ResiliencePipelineBuilder();
        rpb.AddRetry(new()
        {
            //Unclear how these exceptions look.  There are 429001/429002/429003 to deal with, + network issues.
            //Auth failures should not be handled.  We will have make this handler more specific as these occur.
            ShouldHandle = ctx => ValueTask.FromResult(ctx.Outcome.Exception != null),
            BackoffType = DelayBackoffType.Exponential,
            //1 2 4 8 16 30
            Delay = TimeSpan.FromSeconds(1),
            MaxDelay = TimeSpan.FromSeconds(_options.RetryMaxDelaySeconds),
            MaxRetryAttempts = int.MaxValue,
            OnRetry = args =>
            {
                CXTrace.Current.Event(args.Outcome.Exception);
                logger.LogWarning(
                    $"{args.Outcome.Exception?.GetType().Name} {args.Outcome.Exception?.Message} encountered. Retrying in {args.RetryDelay}.");
                return ValueTask.CompletedTask;
            }
        }).AddTimeout(TimeSpan.FromSeconds(_options.RetryTimeoutSeconds));

        _resiliencePipeline = rpb.Build();
    }

    public async Task<string> ProcessLongAsync(string text)
    {
        return await CXTrace.Current.SpanFor(CXTrace.Section_Translate_Chunks,
            new
            {
                Text = text,
                RetryMaxDelaySeconds = _options.RetryMaxDelaySeconds,
                RetryTimeoutSeconds = _options.RetryTimeoutSeconds,
                FailHard = _options.FailHard
            }).ExecuteAsync(async _ =>
        {
            var chunker = new CharLimitSplitter();
            chunker.CharLimit = MaxCharactersPerRequest;
            var sb = new StringBuilder();
            foreach (var chunk in await chunker.ChunkAsync(text))
                if (!string.IsNullOrWhiteSpace(chunk))
                {
                    sb.Append(await ProcessAsync(chunk));
                    sb.Append(' ');
                }

            return sb.ToString();
        });
    }

    public async Task<string> ProcessAsync(string text)
    {
        if (text == null)
            return "";

        if (text.Length > MaxCharactersPerRequest)
            return await ProcessLongAsync(text);

        return await CXTrace.Current.SpanFor(CXTrace.Section_Translate,
            new
            {
                Text = text,
                RetryMaxDelaySeconds = _options.RetryMaxDelaySeconds,
                RetryTimeoutSeconds = _options.RetryTimeoutSeconds,
                FailHard = _options.FailHard
            }).ExecuteAsync(async span =>
        {
            var credential = new AzureKeyCredential(_options.ApiKey);
            var client = new TextTranslationClient(credential, "swedencentral");

            Response<IReadOnlyList<TranslatedTextItem>> response;
            if (_options.FailHard!.Value)
            {
                response = await _resiliencePipeline.ExecuteAsync(async _ => await client.TranslateAsync(_options.TargetLanguage, text));
            }
            else
            {
                try
                {
                    response = await _resiliencePipeline.ExecuteAsync(async _ =>
                        await client.TranslateAsync(_options.TargetLanguage, text));
                }
                catch (Exception ex)
                {
                    CXTrace.Current.Event(ex);
                    span.Output = new { Translated = false, Exception = ex.Message };
                    return text;
                }
            }

            var translations = response.Value;
            var translation = translations.FirstOrDefault();

            if (translation != null)
            {
                if (translation.DetectedLanguage.Language == _options.TargetLanguage &&
                    translation.DetectedLanguage.Confidence >= _options.DontTranslateMinConfidence)
                {
                    span.Output = new
                        { Translated = false, AlreadyInTargetLanguage = true, Confidence = translation.DetectedLanguage.Confidence };
                    return text;
                }

                var res = translation.Translations[0].Text;
                span.Output = new
                {
                    Translated = true,
                    Text = res,
                    InputLanguage = translation.DetectedLanguage.Language,
                    Confidence = translation.DetectedLanguage.Confidence
                };
                return res;
            }

            span.Output = new { Translated = false };
            return text;
        });
    }
}
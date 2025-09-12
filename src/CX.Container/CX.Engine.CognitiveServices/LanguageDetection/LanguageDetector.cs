using System.Text;
using System.Text.Json;
using CX.Engine.Common;
using CX.Engine.Common.Tracing;
using Flurl.Http;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CX.Engine.CognitiveServices.LanguageDetection;

public class LanguageDetector : IDisposable
{
    private readonly string _name;
    private readonly IDisposable _optionsMonitorDisposable;
    private LanguageDetectorOptions _options;

    public LanguageDetector([NotNull] string name, [NotNull] IOptionsMonitor<LanguageDetectorOptions> monitor, ILogger logger, IServiceProvider sp)
    {
        _name = name ?? throw new ArgumentNullException(nameof(name));
        _optionsMonitorDisposable = monitor.Snapshot(() => _options, v => _options = v, logger, sp);
    }

    public Task<DetectLanguageResult> DetectLanguageAsync(string text)
    {
        var opts = _options;
        return CXTrace.Current.SpanFor("detect-language", new { Text = text.Preview(opts.CharacterLimit) }).ExecuteAsync(async span =>
        {

            var response = await $"{opts.Endpoint}"
                .WithHeader("Ocp-Apim-Subscription-Key", opts.ApiKey)
                .WithHeader("Accept", "application/json")
                .AllowAnyHttpStatus()
                .PostAsync(new StringContent(JsonSerializer.Serialize(new
                {
                    documents = new[]
                    {
                        new
                        {
                            id = "1",
                            text = text.Preview(opts.CharacterLimit),
                            countryHint = opts.CountryHint
                        }
                    }
                }), Encoding.UTF8, "application/json"));

            var content = await response.GetStringAsync();

            if (!response.ResponseMessage.IsSuccessStatusCode)
                throw new InvalidOperationException($"Language detection failed. Status: {response.StatusCode}, Error: {content}");

            var detectionResult = JsonSerializer.Deserialize<JsonElement>(content);

            // Parse the response correctly based on the actual API response structure
            if (detectionResult.TryGetProperty("documents", out var documents) &&
                documents.GetArrayLength() > 0 &&
                documents[0].TryGetProperty("detectedLanguage", out var detectedLanguage))
            {
                var language = detectedLanguage.GetProperty("name").GetString();
                var isoCode = detectedLanguage.GetProperty("iso6391Name").GetString();
                var confidence = detectedLanguage.GetProperty("confidenceScore").GetDouble();

                span.Output = new { Language = language, IsoCode = isoCode, Confidence = confidence };
                return new DetectLanguageResult(language, isoCode, confidence);
            }

            throw new InvalidOperationException("Failed to parse language detection response");
        });
    }

    public void Dispose()
    {
        _optionsMonitorDisposable?.Dispose();
    }
}
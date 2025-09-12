using System.Text;
using System.Text.Json;
using CX.Engine.Common;
using CX.Engine.Common.Tracing;
using Flurl.Http;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CX.Engine.CognitiveServices.ToxicityAnalysis;

public class ToxicityAnalyzer : IDisposable
{
    private readonly string _name;
    private readonly IDisposable _optionsMonitorDisposable;
    private ToxicityAnalyzerOptions _options;

    public ToxicityAnalyzer([NotNull] string name, IOptionsMonitor<ToxicityAnalyzerOptions> monitor, ILogger logger, IServiceProvider sp)
    {
        _name = name ?? throw new ArgumentNullException(nameof(name));
        _optionsMonitorDisposable = monitor.Snapshot(() => _options, v => _options = v, logger, sp);
    }

    public Task<AnalyzeToxicityResult> AnalyzeToxicityAsync(string text)
    {
        var opts = _options;
        
        var previewText = text.Preview(opts.CharacterLimit);
        return CXTrace.Current.SpanFor("analyze-toxicity", new { Text = previewText }).ExecuteAsync(async _ =>
        {
            var response = await opts.Endpoint
                .WithHeader("Ocp-Apim-Subscription-Key", opts.ApiKey)
                .WithHeader("Accept", "application/json")
                .AllowAnyHttpStatus()
                .PostAsync(new StringContent(JsonSerializer.Serialize(new
            {
                text = previewText
            }), Encoding.UTF8, "application/json"));

            var content = await response.GetStringAsync();

            if (!response.ResponseMessage.IsSuccessStatusCode)
                throw new InvalidOperationException($"Content safety analysis failed. Status: {response.StatusCode}, Error: {content}");

            var toxicityResult = JsonSerializer.Deserialize<JsonElement>(content);
            var res = new AnalyzeToxicityResult();

            if (toxicityResult.TryGetProperty("categoriesAnalysis", out var categoriesAnalysis))
            {
                foreach (var category in categoriesAnalysis.EnumerateArray())
                {
                    var categoryName = category.GetProperty("category").GetString()!;
                    var severity = category.GetProperty("severity").GetInt32();
                    res.CategoryLevels[categoryName] = severity;
                    // 0 => "None",
                    // 1 => "Low",
                    // 2 => "Medium",
                    // 3 => "High",
                }

                // Check if any category has medium or high severity
                res.ContainsToxicContent = categoriesAnalysis.EnumerateArray()
                    .Any(category => category.GetProperty("severity").GetInt32() >= opts.ThresholdLevel);
            }

            return res;
        });
    }

    public void Dispose()
    {
        _optionsMonitorDisposable?.Dispose();
    }
}
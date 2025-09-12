using System.Text;
using System.Text.Json;
using CX.Engine.Common;
using CX.Engine.Common.Tracing;
using Flurl.Http;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CX.Engine.CognitiveServices.SentimentAnalysis;

public class SentimentAnalyzer : IDisposable
{
    private readonly string _name;
    private readonly IDisposable _optionsMonitorDisposable;
    private SentimentAnalyzerOptions _options;

    public SentimentAnalyzer([NotNull] string name, IOptionsMonitor<SentimentAnalyzerOptions> monitor, ILogger logger, IServiceProvider sp)
    {
        _name = name ?? throw new ArgumentNullException(nameof(name));
        _optionsMonitorDisposable = monitor.Snapshot(() => _options, v => _options = v, logger, sp);
    }

    public static SentimentType ParseSentimentType(string s) =>
        s.ToLower() switch
        {
            "positive" => SentimentType.Positive,
            "neutral" => SentimentType.Neutral,
            "negative" => SentimentType.Negative,
            "mixed" => SentimentType.Mixed,
            _ => throw new InvalidOperationException($"Invalid sentiment type: {s}")
        };

    public Task<AnalyzeSentimentResult> AnalyzeSentimentAsync(string text, bool processSpeakers)
    {
        var opts = _options;
        var previewText = text.Preview(opts.CharacterLimit);
        return CXTrace.Current.SpanFor("analyze-sentiment", new { Text = previewText, ProcessSpeakers = processSpeakers }).ExecuteAsync(async span =>
        {
            var opts = _options;
            var response = await $"{opts.Endpoint}"
                .WithHeader("Ocp-Apim-Subscription-Key", opts.ApiKey)
                .WithHeader("Accept", "application/json")
                .AllowAnyHttpStatus()
                .PostAsync(new StringContent(JsonSerializer.Serialize(new
                {
                    kind = "SentimentAnalysis",
                    analysisInput = new
                    {
                        documents = new[]
                        {
                            new
                            {
                                id = "1",
                                text = previewText,
                                language = "en"
                            }
                        }
                    }
                }), Encoding.UTF8, "application/json"));

            var content = await response.GetStringAsync();

            if (!response.ResponseMessage.IsSuccessStatusCode)
                throw new InvalidOperationException($"Sentiment analysis failed. Status: {response.StatusCode}, Error: {content}");

            var sentimentResult = JsonSerializer.Deserialize<JsonElement>(content);

            if (!sentimentResult.TryGetProperty("results", out var results))
                throw new InvalidOperationException("Failed to get 'results' property from sentiment analysis response");

            if (!results.TryGetProperty("documents", out var documents) || documents.GetArrayLength() == 0)
                throw new InvalidOperationException("No documents found in sentiment analysis response");

            var document = documents[0];

            if (!document.TryGetProperty("sentiment", out var sentimentProperty))
                throw new InvalidOperationException("Failed to get 'sentiment' property from document");

            var res = new AnalyzeSentimentResult();

            var sentiment = sentimentProperty.GetString();
            if (string.IsNullOrEmpty(sentiment))
                throw new InvalidOperationException("Sentiment value is null or empty");

            if (!document.TryGetProperty("confidenceScores", out var confidenceScores))
                throw new InvalidOperationException("Failed to get 'confidenceScores' property from document");

            res.OverallSentiment = ParseSentimentType(sentiment);

            foreach (var score in confidenceScores.EnumerateObject())
            {
                var scoreValue = score.Value.GetDouble();
                res.ConfidenceScores[ParseSentimentType(score.Name)] = scoreValue;
            }

            // Add sentence-level analysis if available
            if (document.TryGetProperty("sentences", out var sentences))
            {
                string lastSpeakerName = null;
                
                foreach (var sentence in sentences.EnumerateArray())
                {
                    if (sentence.TryGetProperty("text", out var sentenceText) &&
                        sentence.TryGetProperty("sentiment", out var sentenceSentiment) &&
                        sentence.TryGetProperty("confidenceScores", out var sentenceScores))
                    {
                        var sentenceContent = sentenceText.GetString()!.Trim();
                        string speakerName = null;

                        if (processSpeakers)
                        {
                            if (sentenceContent.Contains(":"))
                            {
                                var parts = sentenceContent.Split(':');
                                speakerName = parts[0].Trim();
                                lastSpeakerName = speakerName;
                                sentenceContent = parts[1].Trim();
                            }
                            else
                                speakerName ??= lastSpeakerName;
                        }

                        var sentenceSentimentValue = sentenceSentiment.GetString();

                        var sRes = new SentenceAnalysisResult();
                        sRes.Speaker = speakerName;
                        sRes.Sentence = sentenceContent;
                        sRes.Sentiment = ParseSentimentType(sentenceSentimentValue);

                        // Get the highest confidence score
                        var maxScore = 0.0;
                        foreach (var score in sentenceScores.EnumerateObject())
                        {
                            var scoreValue = score.Value.GetDouble();
                            sRes.ConfidenceScores[ParseSentimentType(score.Name)] = scoreValue;
                            maxScore = Math.Max(maxScore, scoreValue);
                        }

                        sRes.ConfidenceScore = maxScore;
                        sRes.Offset = sentence.GetProperty("offset").GetInt32();
                        sRes.Length = sentence.GetProperty("length").GetInt32();

                        res.SentenceAnalysis.Add(sRes);
                    }
                }
            }
            
            res.TotalSentences = res.SentenceAnalysis.Count;
            res.SentimentCounts = res.SentenceAnalysis.GroupBy(s => s.Sentiment)
                .ToDictionary(g => g.Key, g => g.Count());
            
            if (processSpeakers)
                res.SpeakerSentimentCounts = res.SentenceAnalysis
                    .Where(s => s.Speaker != null)
                    .GroupBy(s => s.Speaker)
                    .ToDictionary(g => g.Key, g => g.GroupBy(s => s.Sentiment)
                        .ToDictionary(g2 => g2.Key, g2 => g2.Count()));
            
            span.Output = res;
            return res;
        });
    }

    public void Dispose()
    {
        _optionsMonitorDisposable?.Dispose();
    }
}
using JetBrains.Annotations;

namespace CX.Engine.CognitiveServices.SentimentAnalysis;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class AnalyzeSentimentResult
{
    public SentimentType OverallSentiment { get; set; }
    public Dictionary<SentimentType, double> ConfidenceScores { get; set; } = [];
    public List<SentenceAnalysisResult> SentenceAnalysis { get; set; } = [];
    public Dictionary<string, Dictionary<SentimentType, int>> SpeakerSentimentCounts { get; set; } = [];

    public int TotalSentences { get; set; }
    public Dictionary<SentimentType, int> SentimentCounts { get; set; } = [];
}




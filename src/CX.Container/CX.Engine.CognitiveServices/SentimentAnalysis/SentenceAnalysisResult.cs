using JetBrains.Annotations;

namespace CX.Engine.CognitiveServices.SentimentAnalysis;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class SentenceAnalysisResult
{
    public string Speaker { get; set; }
    public string Sentence { get; set; }
    public SentimentType Sentiment { get; set; }
    public double ConfidenceScore { get; set; }
    public Dictionary<SentimentType, double> ConfidenceScores { get; set; } = [];
    public int Offset { get; set; }
    public int Length { get; set; }
}
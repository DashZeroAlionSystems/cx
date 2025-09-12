using CX.Engine.CognitiveServices.ConversationAnalysis;
using CX.Engine.CognitiveServices.LanguageDetection;
using CX.Engine.CognitiveServices.SentimentAnalysis;
using CX.Engine.CognitiveServices.ToxicityAnalysis;
using CX.Engine.CognitiveServices.Transcriptions;
using JetBrains.Annotations;

namespace CX.Engine.CallAnalysis;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class CallAnalyzerResult
{
    public Transcription Transcription { get; set; }
    public DetectLanguageResult DetectedLanguage { get; set; }
    public AnalyzeSentimentResult Sentiment { get; set; }
    public ConversationAnalyzerResult Summary { get; set; }
    public AnalyzeToxicityResult Toxicity { get; set; }
}
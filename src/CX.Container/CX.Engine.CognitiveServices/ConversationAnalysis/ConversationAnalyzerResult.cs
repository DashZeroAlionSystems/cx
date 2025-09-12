namespace CX.Engine.CognitiveServices.ConversationAnalysis;

public class ConversationAnalyzerResult
{
    public Dictionary<string, object> Aspects { get; set; } = [];
    public List<ConversationAnalyzerWarning> Warnings { get; set; } = [];
}
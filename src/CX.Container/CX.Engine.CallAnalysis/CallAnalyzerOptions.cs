using CX.Engine.Common;

namespace CX.Engine.CallAnalysis;

public class CallAnalyzerOptions : IValidatable
{
    public string TranscriptionServiceName { get; set; }
    public string ChatAgentName { get; set; }
    public string LanguageDetectorName { get; set; }
    public string SentimentAnalyzerName { get; set; }
    public string ConversationAnalyzerName { get; set; }
    public string ToxicityAnalyzerName { get; set; }
    public List<string> SpeakerRoles { get; set; }
    public bool RoleDetectorIncludeReasoning { get; set; }
    public string RoleDetectorPrompt { get; set; }

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(TranscriptionServiceName))
            throw new ArgumentException($"{nameof(TranscriptionServiceName)} is required");
        
        if (string.IsNullOrWhiteSpace(ChatAgentName))
            throw new ArgumentException($"{nameof(ChatAgentName)} is required");
        
        if (string.IsNullOrWhiteSpace(LanguageDetectorName))
            throw new ArgumentException($"{nameof(LanguageDetectorName)} is required");
        
        if (string.IsNullOrWhiteSpace(SentimentAnalyzerName))
            throw new ArgumentException($"{nameof(SentimentAnalyzerName)} is required");
        
        if (string.IsNullOrWhiteSpace(ConversationAnalyzerName))
            throw new ArgumentException($"{nameof(ConversationAnalyzerName)} is required");
        
        if (string.IsNullOrWhiteSpace(ToxicityAnalyzerName))
            throw new ArgumentException($"{nameof(ToxicityAnalyzerName)} is required");

        if (SpeakerRoles == null || SpeakerRoles.Count == 0)
            throw new ArgumentException($"{nameof(SpeakerRoles)} is required");
        
        if (SpeakerRoles.Any(string.IsNullOrWhiteSpace))
            throw new ArgumentException($"{nameof(SpeakerRoles)} cannot contain null or empty values");
        
        if (string.IsNullOrWhiteSpace(RoleDetectorPrompt))
            throw new ArgumentException($"{nameof(RoleDetectorPrompt)} is required");
    }
}
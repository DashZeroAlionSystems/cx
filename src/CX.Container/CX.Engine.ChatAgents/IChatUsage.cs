using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace CX.Engine.ChatAgents;

public interface IChatUsage
{
    public int CompletionTokens { get; set; }
    public int PromptTokens { get; set; }
    public int TotalTokens { get; set; }

    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public class PromptTokensDetailsPOCO
    {
        [JsonPropertyName("cached_tokens")]
        public int CachedTokens { get; set; }
    }
    
    [JsonPropertyName("prompt_tokens_details")]
    public PromptTokensDetailsPOCO PromptTokensDetails { get; set; }

    public class CompletionTokenDetailsPOCO
    {
        [JsonPropertyName("reasoning_tokens")]
        public int ReasoningTokens { get; set; }
        
        [JsonPropertyName("audio_tokens")]
        public int AudioTokens { get; set; }
        
        [JsonPropertyName("accepted_prediction_tokens")]
        public int AcceptedPredictionTokens { get; set; }
        
        [JsonPropertyName("rejected_prediction_tokens")]
        public int RejectedPredictionTokens { get; set; }
    }
    
    [JsonPropertyName("completion_tokens_details")]
    public CompletionTokenDetailsPOCO CompletionTokensDetails { get; set; }
}
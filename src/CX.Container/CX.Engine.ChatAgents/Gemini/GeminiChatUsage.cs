using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace CX.Engine.ChatAgents.Gemini;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class GeminiChatUsage : IChatUsage
{
    [JsonPropertyName("candidatesTokenCount")]
    public int CompletionTokens { get; set; }
    
    [JsonPropertyName("promptTokenCount")]
    public int PromptTokens { get; set; }
    
    [JsonPropertyName("totalTokenCount")]
    public int TotalTokens { get; set; }
    public IChatUsage.PromptTokensDetailsPOCO PromptTokensDetails { get; set; }
    public IChatUsage.CompletionTokenDetailsPOCO CompletionTokensDetails { get; set; }
}
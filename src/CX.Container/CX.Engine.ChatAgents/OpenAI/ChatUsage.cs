using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace CX.Engine.ChatAgents.OpenAI;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class ChatUsage : IChatUsage
{
    [JsonPropertyName("completion_tokens")]
    public int CompletionTokens { get; set; }

    [JsonPropertyName("prompt_tokens")]
    public int PromptTokens { get; set; }

    [JsonPropertyName("total_tokens")]
    public int TotalTokens { get; set; }

    [JsonPropertyName("prompt_tokens_details")]
    public IChatUsage.PromptTokensDetailsPOCO PromptTokensDetails { get; set; }
    [JsonPropertyName("completion_tokens_details")]
    public IChatUsage.CompletionTokenDetailsPOCO CompletionTokensDetails { get; set; }
}
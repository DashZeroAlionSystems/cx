using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace CX.Engine.Common.Embeddings.OpenAI;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class UsageInfo
{
    [JsonPropertyName("prompt_tokens")]
    public int PromptTokens { get; set; }
    [JsonPropertyName("total_tokens")]
    public int TotalTokens { get; set; }
}
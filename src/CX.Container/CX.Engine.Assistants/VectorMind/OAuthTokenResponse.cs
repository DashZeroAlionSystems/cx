using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace CX.Engine.Assistants.VectorMind;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class OAuthTokenResponse
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = null!;

    [JsonPropertyName("token_type")]
    public string TokenType { get; set; } = null!;

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }
    
    [JsonIgnore]
    public DateTime ExpiresAt;
}
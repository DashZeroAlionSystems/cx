using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace CX.Engine.Assistants.VectorMind;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class ApiResponse
{
    [JsonPropertyName("content")]
    public string Message { get; set; }

    [JsonPropertyName("citations")]
    public Citation[] Citations { get; set; }

    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public class Citation
    {
        [JsonPropertyName("url")]
        public string Url { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }
    }
}

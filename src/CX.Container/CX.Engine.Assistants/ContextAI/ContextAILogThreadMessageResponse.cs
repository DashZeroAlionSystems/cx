using System.Text.Json.Serialization;

namespace CX.Engine.Assistants.ContextAI;

public class ContextAILogThreadMessageResponse
{
    [JsonInclude] [JsonPropertyName("status")]
    public string Status;

    [JsonInclude] [JsonPropertyName("data")]
    public ContextAILogThreadMessageResponseData Data;

    public class ContextAILogThreadMessageResponseData
    {
        [JsonInclude] [JsonPropertyName("id")] public string Id;
        [JsonInclude] [JsonPropertyName("provided_id")] public string ProvidedId;
    }
}
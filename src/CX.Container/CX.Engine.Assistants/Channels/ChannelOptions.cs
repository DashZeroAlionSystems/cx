using CX.Engine.Common;
using CX.Engine.Common.Json;
using Newtonsoft.Json;

namespace CX.Engine.Assistants.Channels;

public class ChannelOptions
{
    public string AssistantName { get; set; } = null!;
    public string SystemPromptOverride { get; set; } = null!;
    
    [UseJsonDocumentSetup]
    [UseNewInstanceForDefaultValue]
    [JsonConverter(typeof(ComponentsConverter<AgentOverride>))]
    [JsonProperty("Components")]
    public Components<AgentOverride> Overrides { get; set; } = [];
    public bool ShowInUI { get; set; }
    public string DisplayName { get; set; }
}
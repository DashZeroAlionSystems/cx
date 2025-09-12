using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using CX.Engine.ChatAgents;
using CX.Engine.ChatAgents.OpenAI;
using JetBrains.Annotations;

namespace CX.Engine.Assistants.FlatQuery;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public sealed class FlatQueryAssistantOptionsOverrides : AgentOverride
{
    public bool OverrideAnswerSmartFormat { get; set; }
    public string AnswerSmartFormat { get; set; }
    
    public bool OverrideResponseSchema { get; set; }
    public SchemaResponseFormat ResponseFormatBase { get; set; }

    public bool OverrideSemanticFilterOutToAnswerPrompt { get; set; }
    public string SemanticFilterOutToAnswerPrompt { get; set; }
    
    public bool OverrideStripRegex { get; set; }
    public string StripRegex { get; set; }

    public int? RowLimit { get; set; }
    public int? SemanticRowLimit { get; set; }
    
    [JsonInclude]
    internal JsonNode JsonEOutputTemplate { get; set; }
}
using CX.Engine.ChatAgents;
using CX.Engine.ChatAgents.OpenAI;
using CX.Engine.Common;
using JetBrains.Annotations;

namespace CX.Engine.Assistants;

[UniqueComponent]
[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public sealed class ResponseFormatOverride : AgentOverride
{
    public SchemaResponseFormat ResponseFormat { get; set; }

    public ResponseFormatOverride()
    {
    }

    public ResponseFormatOverride(SchemaResponseFormat responseFormat)
    {
        ResponseFormat = responseFormat;
    }
}

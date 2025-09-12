using System.Text.Json.Serialization;
using CX.Engine.Common;
using CX.Engine.Common.Json;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace CX.Engine.Assistants.PgTableEnrichment;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class PgTableEnrichmentAssistantOptions : IValidatable
{
    [JsonInclude]
    [UseJsonDocumentSetup] public List<PgTableEnrichmentOperation> Operations;

    public string ChatAgentName { get; set; }
    public string Prompt { get; set; }    

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(ChatAgentName))
            throw new InvalidOperationException($"{nameof(ChatAgentName)} is required.");
        
        if (string.IsNullOrWhiteSpace(Prompt))
            throw new InvalidOperationException($"{nameof(Prompt)} is required.");
        
        if (Operations == null || Operations.Count == 0)
            throw new InvalidOperationException($"{nameof(Operations)} is required with at least one operation.");
        
        foreach (var op in Operations)
            op.Validate();
    }
}
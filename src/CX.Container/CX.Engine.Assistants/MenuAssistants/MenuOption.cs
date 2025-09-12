using CX.Engine.Common;
using JetBrains.Annotations;

namespace CX.Engine.Assistants.MenuAssistants;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class MenuOption : IValidatable
{
    public string AgentId { get; set; }
    public int Priority { get; set; }
    public string ChannelName { get; set; }
    public string Prompt { get; set; }
    
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(AgentId))
            throw new InvalidOperationException($"{nameof(AgentId)} is required.");

        if (string.IsNullOrWhiteSpace(ChannelName))
            throw new InvalidOperationException($"{nameof(ChannelName)} is required.");
        
        if (string.IsNullOrWhiteSpace(Prompt))
            throw new InvalidOperationException($"{nameof(Prompt)} is required.");
    }
}
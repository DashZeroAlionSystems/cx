using CX.Engine.Assistants;
using JetBrains.Annotations;

namespace CX.Engine.Discord;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class DiscordChannelOptions
{
    public string Name;
    public ulong DiscordId { get; set; }
    public bool Active { get; set; }
    public string AssistantName { get; set; }
    public string SystemPrompt { get; set; }
    public string ContextualizePrompt { get; set; }
    public string GoogleSheetId { get; set; }
    public string QuizPrompt { get ; set; }
    
    public IAssistant Assistant;
    
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Name))
            throw new InvalidOperationException("Channel name is required.");
        
        if (string.IsNullOrWhiteSpace(AssistantName))
            throw new InvalidOperationException($"Assistant name is required for channel {Name}.");
    }
}
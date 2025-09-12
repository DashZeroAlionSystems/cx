namespace CX.Engine.Assistants.ArtifactAssists;

public class PromptAction
{
    public string ShortSignature;
    public string UsageNotes;
    public AgentAction Action;
    
    public PromptAction(string shortSignature, string usageNotes = null, AgentAction action = null)
    {
        ShortSignature = shortSignature;
        UsageNotes = usageNotes;
        Action = action;
    }
}
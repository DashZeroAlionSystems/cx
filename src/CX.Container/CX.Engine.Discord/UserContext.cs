using CX.Engine.Assistants;

namespace CX.Engine.Discord;

public class UserContext
{
    public string UserId;
    public string Question;
    public string Answer;
    public List<RankedChunk> Chunks;
    public readonly Dictionary<ulong, AgentRequest> AssistantContexts = new();

    public AgentRequest GetAssistantContextForChannel(ulong channel)
    {
        lock (AssistantContexts)
        {
            if (!AssistantContexts.ContainsKey(channel))
                AssistantContexts[channel] = new() { UserId = UserId };
            return AssistantContexts[channel];
        }
    }

    public void Clear()
    {
        Question = null;
        Answer = null;
        Chunks = null;
    }
}
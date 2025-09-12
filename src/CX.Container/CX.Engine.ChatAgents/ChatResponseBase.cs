using CX.Engine.ChatAgents.OpenAI;
using CX.Engine.Common;
using Microsoft.Extensions.Logging;

namespace CX.Engine.ChatAgents;

public abstract class ChatResponseBase
{
    public readonly List<IChatChoice> Choices = [];
    public List<AttachmentInfo> InputAttachments;
    public long Created;

    public string Id;

    public string Model;

    public string Object;
    public TimeSpan ResponseTime { get; set; }

    public string SystemPrompt;

    public IChatUsage ChatUsage;

    public abstract bool IsRefusal { get; }

    public abstract string Answer { get; set; }

    public abstract void PopulateFromBytes(byte[] bytes);

    public abstract ChatResponse ToChatResponse(ILogger logger, OpenAIChatAgentOptions options);
}
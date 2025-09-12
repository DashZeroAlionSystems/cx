using System.Text.Json;
using CX.Engine.Common;
using CX.Engine.TextProcessors.Splitters;
using JetBrains.Annotations;

namespace CX.Engine.ChatAgents;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public abstract class ChatRequestBase
{
    public IChatAgent Agent;
    public List<TextChunk> Chunks { get; set; }
    public string Name { get; set; }
    public string Question { get; set; }
    public string SystemPrompt { get; set; }
    public string ReasoningEffort { get; set; } 
    public string ContextualizePrompt { get; set; }
    public int? MaxCompletionTokens { get; set; }
    public bool UseAttachments { get; set; } = true;

    public List<string> StringContext { get; } = [];
    public readonly List<AttachmentInfo> Attachments = [];
    public List<ChatMessage> History = [];
    public abstract SchemaResponseFormat ResponseFormatBase { get; set; }

    public abstract void SetResponseSchema(SchemaBase schema);
    public abstract void SetResponseSchema(JsonElement? schema);
    
    public string PredictedOutput { get; set; }
    public TimeSpan? TimeOut { get; set; }
    public int? MaxRetries { get; set; }
    public TimeSpan? MaxDelay { get; set; }
    public TimeSpan? MinDelay { get; set; }
    public string ImageUrl { get; set; }
    public abstract void Serialize(Utf8JsonWriter jw);

    protected ChatRequestBase(string question, List<TextChunk> chunks = null, string systemPrompt = null)
    {
        Question = question;
        Chunks = chunks ?? [];
        SystemPrompt = systemPrompt;
    }

    protected ChatRequestBase(IChatAgent agent, string question, List<TextChunk> chunks = null)
    {
        Agent = agent;
        Question = question ?? throw new ArgumentNullException(nameof(question));
        Chunks = chunks ?? [];
    }

    protected ChatRequestBase()
    {
        
    }
}

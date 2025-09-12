using System.Text.Json;
using System.Text.Json.Serialization;

namespace CX.Engine.ChatAgents;

public abstract class ChatMessage
{
    [JsonInclude] public string Content;

    [JsonInclude] public string ImageUrl;

    [JsonInclude] public string Role;

    [JsonInclude] public string Refusal;
    
    [JsonInclude] public List<ToolCall> ToolCalls;

    protected ChatMessage()
    {
        Content = null!;
        Role = null!;
    }
    
    public ChatMessage(string role, string content, string imageUrl = null)
    {
        Role = role ?? throw new ArgumentNullException(nameof(role));
        Content = content ?? throw new ArgumentNullException(nameof(content));
        ImageUrl = imageUrl;
    }
    
    public virtual void Serialize(Utf8JsonWriter jw) => throw new NotSupportedException();
}
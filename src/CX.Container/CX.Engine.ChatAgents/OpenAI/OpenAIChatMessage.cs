using System.Text;
using System.Text.Json;
using CX.Engine.Common.Json;

namespace CX.Engine.ChatAgents.OpenAI;

public class OpenAIChatMessage : ChatMessage, ISerializeJson
{

    private OpenAIChatMessage()
    {
        Content = null!;
        Role = null!;
        ToolCalls = new();
    }

    public OpenAIChatMessage(string role, string content, List<ToolCall> toolCalls = null, string imageUrl = null)
    {
        Role = role ?? throw new ArgumentNullException(nameof(role));
        
        if (content == null)
            throw new ArgumentNullException(nameof(content));
        
        Content = content;
        ImageUrl = imageUrl;
        ToolCalls = toolCalls ?? new();
    }

    public override void Serialize(Utf8JsonWriter jw)
    {
        jw.WriteStartObject();
        jw.WritePropertyName("role");
        jw.WriteStringValue(Role);
        
        jw.WritePropertyName("content");

        if (ImageUrl != null)
        {
            jw.WriteStartArray();
            jw.WriteStartObject();
            jw.WriteString("type", "text");
            jw.WriteString("text", Content);
            jw.WriteEndObject();
            jw.WriteStartObject();
            jw.WriteString("type", "image_url");
            jw.WritePropertyName("image_url");
            jw.WriteStartObject();
            jw.Flush();
            jw.WritePropertyName("url"u8);
            jw.WriteRawValue(Encoding.UTF8.GetBytes($"\"{ImageUrl}\""));
            jw.Flush();
            jw.WriteEndObject();
            jw.WriteEndObject();
            jw.WriteEndArray();
        }
        else
        {
            jw.WriteStringValue(Content);
        }

        if (ToolCalls.Count > 0)
        {
            jw.WritePropertyName("tool_calls");
            jw.WriteStartArray();
            foreach (var tc in ToolCalls)
                tc.Serialize(jw);
            jw.WriteEndArray();
        }

        jw.WriteEndObject();
    }

    public static ChatMessage FromJsonReader(ref Utf8JsonReader jr)
    {
        var message = new OpenAIChatMessage();
        // ReSharper disable VariableHidesOuterVariable
        jr.ReadObjectProperties(message,
            false,
            (ref Utf8JsonReader jr, ChatMessage message, string propertyName) =>
                // ReSharper restore VariableHidesOuterVariable
            {
                switch (propertyName)
                {
                    case "content":
                        message.Content = jr.ReadStringValue()!;
                        break;
                    case "role":
                        message.Role = jr.ReadStringValue()!;
                        break;
                    case "refusal":
                        message.Refusal = jr.ReadStringValue()!;
                        break;
                    case "tool_calls":
                        jr.ReadArrayOfObject(true,
                            (ref Utf8JsonReader jr) =>
                            {
                                var tc = new ToolCall();
                                // ReSharper disable once VariableHidesOuterVariable
                                jr.ReadObjectProperties(tc,
                                    false,
                                    (ref Utf8JsonReader jr, ToolCall tc, string propName) =>
                                    {
                                        // ReSharper restore VariableHidesOuterVariable
                                        switch (propName)
                                        {
                                            case "id":
                                                tc.Id = jr.ReadStringValue()!;
                                                break;
                                            case "function":
                                                // ReSharper disable VariableHidesOuterVariable
                                                jr.ReadObjectProperties(tc,
                                                    true,
                                                    (ref Utf8JsonReader jr, ToolCall tc, string propName) =>
                                                        // ReSharper restore VariableHidesOuterVariable
                                                    {
                                                        switch (propName)
                                                        {
                                                            case "name":
                                                                tc.Name = jr.ReadStringValue()!;
                                                                break;
                                                            case "arguments":
                                                                tc.Arguments = jr.ReadStringValue()!;
                                                                break;
                                                            default:
                                                                jr.SkipPropertyValue();
                                                                break;
                                                        }
                                                    });
                                                break;
                                            default:
                                                jr.SkipPropertyValue();
                                                break;
                                        }
                                    });
                                message.ToolCalls.Add(tc);
                            });
                        break;
                    default:
                        jr.SkipPropertyValue();
                        break;
                }
            });

        return message;
    }

    public override string ToString() => Content;
}
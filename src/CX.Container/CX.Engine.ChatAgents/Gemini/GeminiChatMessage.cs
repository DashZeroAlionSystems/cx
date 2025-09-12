using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using CX.Engine.Common.Json;

namespace CX.Engine.ChatAgents.Gemini;

public class GeminiChatMessage : ChatMessage, ISerializeJson
{
    protected GeminiChatMessage() {}
    public GeminiChatMessage(string role, string content, string imageUrl = null) : base(role, content, imageUrl)
    {
    }

    public override void Serialize(Utf8JsonWriter jw)
    {
        jw.WriteStartObject();
        jw.WriteString("role", Role);
        // jw.WritePropertyName("role");
        // jw.WriteStringValue(Role);
        
        jw.WriteStartArray("parts");
        jw.WriteStartObject();
        jw.WritePropertyName("text");

        /*if (ImageUrl != null)
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
        {*/
            jw.WriteStringValue(Content);
        /*}*/

        jw.WriteEndObject();
        jw.WriteEndArray();
        jw.WriteEndObject();
    }

    public static GeminiChatMessage FromJsonReader(ref Utf8JsonReader jr)
    {
        var message = new GeminiChatMessage();
        // ReSharper disable VariableHidesOuterVariable
        jr.ReadObjectProperties(message,
            false,
            (ref Utf8JsonReader jr, GeminiChatMessage message, string propertyName) =>
                // ReSharper restore VariableHidesOuterVariable
            {
                switch (propertyName)
                {
                    case "text":
                        message.Content = jr.ReadStringValue()!;
                        break;
                    case "code":
                        message.Content = jr.ReadStringValue()!;
                        FromJsonReader(ref jr);
                        break;
                    case "role":
                        message.Role = jr.ReadStringValue()!;
                        break;
                    case "message":
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
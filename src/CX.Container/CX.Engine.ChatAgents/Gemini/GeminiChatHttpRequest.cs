using System.Text.Json;
using CX.Engine.Common.Json;
using Json.More;

namespace CX.Engine.ChatAgents.Gemini;

public class GeminiChatHttpRequest : ISerializeJson
{
    public string Model;
    public string PredictedOutput;
    public int? MaxCompletionTokens;

    public readonly List<GeminiChatMessage> Messages = new();

    public double Temperature;

    public HashSet<ChatTool> Tools;

    public GeminiSchemaResponseFormat ResponseFormat;

    public void Serialize(Utf8JsonWriter jw)
    {
        jw.WriteStartObject();
        jw.WriteStartArray("contents");

        /*if (PredictedOutput != null)
        {
            jw.WriteStartObject("prediction"u8);
            jw.WriteString("type"u8, "content"u8);
            jw.WriteString("content"u8, PredictedOutput);
            jw.WriteEndObject();
        }*/
        
        foreach (var message in Messages)
        {
            message.Serialize(jw);
        }
        jw.WriteEndArray();

        /*if (MaxCompletionTokens > 0)
            jw.WriteNumber("max_completion_tokens"u8, MaxCompletionTokens.Value);*/

        /*if (Tools?.Count > 0)
        {
            jw.WritePropertyName("tools");
            jw.WriteStartArray();

            jw.WriteStartObject();
            jw.WritePropertyName("type");
            jw.WriteStringValue("function");
            jw.WritePropertyName("function");
            jw.WriteStartObject();
            jw.WritePropertyName("name");
            jw.WriteStringValue("skip");
            jw.WritePropertyName("description");
            jw.WriteStringValue("Does nothing");
            jw.WritePropertyName("parameters");
            jw.WriteStartObject();
            jw.WritePropertyName("type");
            jw.WriteStringValue("object");
            jw.WritePropertyName("properties");
            jw.WriteStartObject();
            jw.WriteEndObject();
            jw.WriteEndObject();
            jw.WriteEndObject();
            jw.WriteEndObject();

            foreach (var tool in Tools)
            {
                if (tool != ChatTool.Attach)
                    throw new InvalidOperationException("Only Attach tools are supported.");

                jw.WriteStartObject();
                jw.WritePropertyName("type");
                jw.WriteStringValue("function");
                jw.WritePropertyName("function");
                jw.WriteStartObject();
                jw.WritePropertyName("name");
                jw.WriteStringValue("attach");
                jw.WritePropertyName("description");
                jw.WriteStringValue("Attaches a file, source document or page image to the response");
                jw.WritePropertyName("parameters");
                jw.WriteStartObject();
                jw.WritePropertyName("type");
                jw.WriteStringValue("object");
                jw.WritePropertyName("properties");
                jw.WriteStartObject();
                jw.WritePropertyName("file_id");
                jw.WriteStartObject();
                jw.WritePropertyName("type");
                jw.WriteStringValue("string");
                jw.WritePropertyName("description");
                jw.WriteStringValue("The file_id of the file to attach");
                jw.WriteEndObject();
                jw.WriteEndObject();
                jw.WriteEndObject();
                jw.WriteEndObject();
                jw.WriteEndObject();
            }

            jw.WriteEndArray();
        }*/

        if (ResponseFormat != null)
        {
            jw.WritePropertyName("generationConfig");
            jw.WriteStartObject();
            jw.WritePropertyName("temperature");
            jw.WriteNumberValue(Temperature);
            jw.WriteString("response_mime_type"u8, "application/json"u8);
            jw.WritePropertyName("response_schema"u8);
            ResponseFormat.Serialize(jw);
            jw.WriteEndObject();
        }

        jw.WriteEndObject();
    }
}
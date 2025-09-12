using System.Text.Json;
using CX.Engine.Common.Json;

namespace CX.Engine.ChatAgents.OpenAI;

public class ChatRequest : ISerializeJson
{
    public string Model;
    public string PredictedOutput;
    public string ReasoningEffort;
    public int? MaxCompletionTokens;

    public readonly List<OpenAIChatMessage> Messages = new();

    public double Temperature;

    public HashSet<ChatTool> Tools;

    public OpenAISchemaResponseFormat ResponseFormat;

    public void Serialize(Utf8JsonWriter jw)
    {
        jw.WriteStartObject();
        jw.WritePropertyName("model");
        jw.WriteStringValue(Model);
        jw.WritePropertyName("messages");
        jw.WriteStartArray();
        foreach (var message in Messages)
            message.Serialize(jw);

        jw.WriteEndArray();

        if (PredictedOutput != null)
        {
            jw.WriteStartObject("prediction"u8);
            jw.WriteString("type"u8, "content"u8);
            jw.WriteString("content"u8, PredictedOutput);
            jw.WriteEndObject();
        }

        if (MaxCompletionTokens > 0)
            jw.WriteNumber("max_completion_tokens"u8, MaxCompletionTokens.Value);

        jw.WritePropertyName("temperature");
        jw.WriteNumberValue(Temperature);
        if (Tools?.Count > 0)
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
        }

        if (ResponseFormat != null)
        {
            jw.WritePropertyName("response_format");
            ResponseFormat.Serialize(jw);
        }

        if (ReasoningEffort != null)
            jw.WriteString("reasoning_effort", ReasoningEffort);

        jw.WriteEndObject();
    }
}
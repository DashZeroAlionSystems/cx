using System.Text;
using System.Text.Json;
using CX.Engine.ChatAgents.OpenAI;
using CX.Engine.ChatAgents.OpenAI.Schemas;
using CX.Engine.Common.Json;
using CX.Engine.TextProcessors.Splitters;
using JetBrains.Annotations;

namespace CX.Engine.ChatAgents;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class OpenAIChatRequest : ChatRequestBase, ISerializeJson
{
    public readonly HashSet<ChatTool> Tools = [];
    public OpenAISchemaResponseFormat ResponseFormat { get; set; }
    public override SchemaResponseFormat ResponseFormatBase
    {
        get => ResponseFormat;
        set { 
            if (value is not OpenAISchemaResponseFormat openAISchemaResponseFormat)
                throw new InvalidOperationException($"Expected {nameof(OpenAISchemaResponseFormat)} but got {value?.GetType().Name}");
            
            ResponseFormat = openAISchemaResponseFormat; 
        }
    }

    public override void SetResponseSchema(SchemaBase schema)
    {
        if (schema is not OpenAISchema openAISchema)
            throw new InvalidOperationException($"Expected {nameof(OpenAISchema)} but got {schema?.GetType().Name}");
        ResponseFormat = openAISchema;
    }

    public override void SetResponseSchema(JsonElement? schema)
    {
        if (schema == null)
            ResponseFormat = null;
        else
            ResponseFormat = new(schema);
    }

    public OpenAIChatRequest(IChatAgent agent, string question, List<TextChunk> chunks = null, string systemPrompt = null): base(question, chunks, systemPrompt)
    {
        Agent = agent ?? throw new ArgumentNullException(nameof(agent));
    }


    public async Task<ChatRequestBase> AttachImageAsync(Stream stream)
    {
        // Create a byte array of the size of the stream
        var bytes = new byte[stream.Length];
        stream.Position = 0;
        // Read the file into the byte array
        _ = await stream.ReadAsync(bytes, 0, (int)stream.Length);
        // Convert the byte array to a base64 string for use in a data URL
        var base64File = Convert.ToBase64String(bytes);
        ImageUrl = $"data:image/jpeg;base64,{base64File}";
        return this;
    }

    public override void Serialize(Utf8JsonWriter jw)
    {
        jw.WriteStartObject();
        jw.WritePropertyName("question");
        jw.WriteStringValue(Question);
        if (ImageUrl != null)
            jw.WriteStringValue(ImageUrl);
        
        if (Agent != null)
            jw.WriteString("Model", Agent.Model);

        if (SystemPrompt != null)
        {
            jw.WritePropertyName("systemPrompt");
            jw.WriteStringValue(SystemPrompt);
        }

        if (StringContext?.Count > 0)
        {
            jw.WritePropertyName("stringContext");
            jw.WriteStartArray();
            foreach (var context in StringContext)
            {
                jw.WriteStringValue(context);
            }

            jw.WriteEndArray();
        }

        if (Chunks?.Count > 0)
        {
            jw.WritePropertyName("chunks");
            jw.WriteStartArray();
            foreach (var chunk in Chunks)
            {
                chunk.Serialize(jw);
            }

            jw.WriteEndArray();
        }

        if (Tools?.Count > 0)
        {
            jw.WritePropertyName("tools");
            jw.WriteStartArray();
            foreach (var tool in Tools)
            {
                jw.WriteStringValue(tool.ToString());
            }

            jw.WriteEndArray();
        }

        if (Attachments?.Count > 0)
        {
            jw.WritePropertyName("attachments");
            jw.WriteStartArray();
            foreach (var attachment in Attachments)
                attachment.Serialize(jw);
            jw.WriteEndArray();
        }

        if (History?.Count > 0)
        {
            jw.WritePropertyName("history");
            jw.WriteStartArray();
            foreach (var message in History)
                message.Serialize(jw);
            jw.WriteEndArray();
        }
        
        if (ResponseFormat != null)
        {
            jw.WritePropertyName("response_format");
            ResponseFormat.Serialize(jw);
        }

        if (ReasoningEffort != null)
        {
            jw.WriteString("reasoning_effort", ReasoningEffort);
        }


        if (MaxCompletionTokens != null)
            jw.WriteNumber("max_completion_tokens", MaxCompletionTokens.Value);
        
        if (PredictedOutput != null)
            jw.WriteString("predicted_output", PredictedOutput);

        jw.WriteEndObject();
    }

    public string GetCacheKey() => this.GetJsonString();
    
    public string GetQueryEmbeddingString()
    {
        var sb = new StringBuilder();
        sb.AppendLine("System: " + SystemPrompt);
        foreach (var msg in History)
            sb.AppendLine($"{msg.Role}: {msg.Content}");
        sb.AppendLine("User:" + Question);
        return Question;
    }

    public void Validate()
    {
        if (Agent == null)
            throw new InvalidOperationException($"{nameof(OpenAIChatRequest)}.{nameof(Agent)} is not set");
    }
}

public class OpenAIChatRequest<T> : OpenAIChatRequest
{
    public OpenAIChatRequest(string question, IChatAgent agent, List<TextChunk> chunks = null, string systemPrompt = null) : base(agent, question, chunks)
    {
        ResponseFormat = new OpenAISchema<T>();
        SystemPrompt = systemPrompt;
    }
}
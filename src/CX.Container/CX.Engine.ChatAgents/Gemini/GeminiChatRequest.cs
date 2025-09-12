using System.Text;
using System.Text.Json;
using CX.Engine.ChatAgents.Gemini.Schemas;
using CX.Engine.Common.Json;
using CX.Engine.TextProcessors.Splitters;
using JetBrains.Annotations;

namespace CX.Engine.ChatAgents.Gemini;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class GeminiChatRequest : ChatRequestBase, ISerializeJson
{
    private string _geminiSchemaPath;
    public string GeminiSchemaPath
    {
        get => _geminiSchemaPath;
        set
        {
            _geminiSchemaPath = value;
            if(ResponseFormat is not null)
                ResponseFormat.GeminiSchemaPath = _geminiSchemaPath;
        }
    }
   
    private GeminiSchemaResponseFormat _responseFormatBase;
    public GeminiSchemaResponseFormat ResponseFormat
    {
        get => _responseFormatBase;
        set
        {
            if(value.GeminiSchemaPath == null && GeminiSchemaPath != null)
                value.GeminiSchemaPath = GeminiSchemaPath;
            _responseFormatBase = value;
        }
    }

    public override SchemaResponseFormat ResponseFormatBase
    {
        get => ResponseFormat;
        set {
            if (value is not GeminiSchemaResponseFormat geminiSchemaResponseFormat)
                throw new InvalidOperationException($"Expected {nameof(GeminiSchemaResponseFormat)} but got {value?.GetType().Name}");
            ResponseFormat = geminiSchemaResponseFormat;
        }
    }

    public override void SetResponseSchema(SchemaBase schema)
    {
        if (schema is not GeminiSchema geminiSchema)
            throw new InvalidOperationException($"Expected {nameof(GeminiSchema)} but got {schema?.GetType().Name}");
        ResponseFormat = geminiSchema;
    }
    
    public override void SetResponseSchema(JsonElement? schema)
    {
        if (schema == null)
            ResponseFormat = null;
        else
            ResponseFormat = new(schema);
    }

    public GeminiChatRequest(string question, List<TextChunk> chunks = null, string systemPrompt = null): base(question, chunks, systemPrompt)
    {
    }

    public GeminiChatRequest()
    {
        
    }
    
    public async Task<GeminiChatRequest> AttachImageAsync(Stream stream)
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
            ResponseFormat.GeminiSchemaPath = GeminiSchemaPath;
            ResponseFormat.Serialize(jw);
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
            throw new InvalidOperationException($"{nameof(GeminiChatRequest)}.{nameof(Agent)} is not set");
    }
}

public class GeminiChatRequest<T> : GeminiChatRequest
{
    public GeminiChatRequest(string question, List<TextChunk> chunks, string systemPrompt = null) : base(question, chunks, systemPrompt)
    {
        ResponseFormat = new GeminiSchema<T>();
        SystemPrompt = systemPrompt;
    }
}
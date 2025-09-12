using System.Reflection;
using System.Text.Json;
using CX.Engine.Common.JsonSchemas;

namespace CX.Engine.ChatAgents.OpenAI.Schemas;

public class OpenAISchema : SchemaBase
{
    public OpenAISchema(string name, string description = null, SchemaObject obj = null) : base(name, description, obj)
    {
    }

    public override void Serialize(Utf8JsonWriter jw)
    {
        if (Object == null)
            throw new InvalidOperationException("A schema must have an object.");
        
        jw.WriteStartObject();
        jw.WriteString("name"u8, Name);
        
        if (Description != null)
            jw.WriteString("description"u8, Description);
        
        jw.WriteBoolean("strict"u8, true);
        jw.WritePropertyName("schema"u8);
        Object.Serialize(jw);
        jw.WriteEndObject();
    }

    public string SerializeToString()
    {
        using var ms = new MemoryStream();
        using var jw = new Utf8JsonWriter(ms);
        Serialize(jw);
        jw.Flush();
        ms.Flush();
        ms.Seek(0, SeekOrigin.Begin);
        using var sr = new StreamReader(ms);
        return sr.ReadToEnd();
    }

    public OpenAISchema Set(Action<SchemaObject> action)
    {
        action(Object);
        return this;
    }

    public OpenAISchema Constrain(string propertyName, List<string> choices)
    {
        if (!Object.Properties.TryGetValue(propertyName, out var property))
            throw new InvalidOperationException($"The property {propertyName} does not exist.");
        
        property.Choices = choices;
        return this;
    }

    public static implicit operator OpenAISchemaResponseFormat(OpenAISchema openAiSchema) => new(openAiSchema);
}

public class OpenAISchema<T> : OpenAISchema
{
    public OpenAISchema() : base(typeof(T).Name)
    {
        var desc = typeof(T).GetCustomAttribute<SemanticAttribute>();
        
        Description = desc?.Description;
        
        Object.AddPropertiesFrom<T>();
    }
}
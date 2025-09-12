using System.Reflection;
using System.Text.Json;
using CX.Engine.Common.JsonSchemas;

namespace CX.Engine.ChatAgents.Gemini.Schemas;

public class GeminiSchema : SchemaBase
{
    public GeminiSchema(string name, string description = null, SchemaObject obj = null) : base(name, description, obj)
    {
    }

    public override void Serialize(Utf8JsonWriter jw)
    {
        if (Object == null)
            throw new InvalidOperationException("A schemaBase must have an object.");
        
        Object.Serialize(jw, true, false, true);
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

    public GeminiSchema Set(Action<SchemaObject> action)
    {
        action(Object);
        return this;
    }

    public GeminiSchema Constrain(string propertyName, List<string> choices)
    {
        if (!Object.Properties.TryGetValue(propertyName, out var property))
            throw new InvalidOperationException($"The property {propertyName} does not exist.");
        
        property.Choices = choices;
        return this;
    }
    
    public static implicit operator GeminiSchemaResponseFormat(GeminiSchema schema) => new(schema);
}

public class GeminiSchema<T> : GeminiSchema
{
    public GeminiSchema() : base(typeof(T).Name)
    {
        var desc = typeof(T).GetCustomAttribute<SemanticAttribute>();
        
        Description = desc?.Description;
        
        Object.AddPropertiesFrom<T>();
    }
}
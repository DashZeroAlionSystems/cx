using System.Text.Json;
using System.Text.Json.Serialization;
using CX.Engine.ChatAgents.Gemini.Schemas;
using CX.Engine.ChatAgents.OpenAI.Schemas;
using CX.Engine.Common.JsonSchemas;

namespace CX.Engine.ChatAgents;

[JsonDerivedType(typeof(GeminiSchema), nameof(GeminiSchema))]
[JsonDerivedType(typeof(OpenAISchema), nameof(OpenAISchema))]
public abstract class SchemaBase
{
    protected SchemaBase(string name, string description = null, SchemaObject obj = null)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Description = description;
        Object = obj ?? new();
    }
    public string Name { get; }
    public string Description { get; set; }
    public SchemaObject Object { get; set; }
    public virtual void Serialize(Utf8JsonWriter jw) => throw new NotSupportedException();

    public string ToJsonString()
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

    public override int GetHashCode() => throw new NotSupportedException();
    public override bool Equals(object x)
    {
        // Check if x is null
        if (x is null)
            return false;

        // Check if x is Schema type
        if (x is not SchemaBase schemaX)
            return false;  // Changed from throwing exception to returning false, as per best practices

        // Handle empty/null cases
        bool descriptionsEqual = (string.IsNullOrEmpty(Description) && string.IsNullOrEmpty(schemaX.Description)) || 
                                 Description?.Equals(schemaX.Description) == true;
                           
        bool namesEqual = (string.IsNullOrEmpty(Name) && string.IsNullOrEmpty(schemaX.Name)) || 
                          Name?.Equals(schemaX.Name) == true;
                     
        bool objectsEqual = (Object is null && schemaX.Object is null) || (Object?.Equals(schemaX.Object) == true);

        return descriptionsEqual && namesEqual && objectsEqual;
    }
}
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using CX.Engine.ChatAgents.Gemini.Schemas;
using Json.More;

namespace CX.Engine.ChatAgents.Gemini;

public class GeminiSchemaResponseFormat: SchemaResponseFormat
{
    protected GeminiSchemaResponseFormat()
    {}
    public GeminiSchemaResponseFormat(GeminiSchema typedSchema): base(typedSchema)
    {
    }
    
    
    public GeminiSchemaResponseFormat(JsonElement? rawSchema): base(rawSchema)
    {
    }
    
    
    public GeminiSchemaResponseFormat(JsonNode rawSchemaNode): base(rawSchemaNode)
    {
    }
    
    private JsonNode NavigatePath(JsonNode current, string path)
    {
        // Suppose path looks like "schema.properties.foo"
        // split on '.' and navigate
        var segments = path.Split('.');
        foreach (var segment in segments)
        {
            if (current is JsonObject obj && obj[segment] != null)
                current = obj[segment];
        }
        return current;
    }
    
    public override void Serialize(Utf8JsonWriter jw)
    {
        var pathToRemove = "additionalProperties";
        
        if (TypedSchema != null)
            TypedSchema.Serialize(jw);
        else if (RawSchemaNode != null)
        {
            // Navigate to the node at "schema" or "schema.properties" etc.
            var targetNode = NavigatePath(RawSchemaNode, GeminiSchemaPath.Replace("$.", ""));
            if (targetNode is JsonObject targetObj)
            {
                // Remove "additionalProperties"
                targetObj.Remove(pathToRemove);
                RawSchemaNode = targetObj;
            }
            RawSchemaNode.WriteTo(jw);
        }
        else
        {
            var targetNode = NavigatePath(RawSchema!.Value.AsNode(), GeminiSchemaPath?.Replace("$.", "") ?? "");
            if (targetNode is JsonObject targetObj)
            {
                // Remove "additionalProperties"
                targetObj.Remove(pathToRemove);
                RawSchema = JsonSerializer.SerializeToElement(targetObj);
            }
            RawSchema!.Value.WriteTo(jw);
        }
    }

    public override string ToString()
    {
        var ms = new MemoryStream();
        var jw = new Utf8JsonWriter(ms);
        Serialize(jw);
        jw.Flush();
        return Encoding.UTF8.GetString(ms.ToArray());
    }
    
    public static implicit operator GeminiSchemaResponseFormat(JsonNode doc) => doc == null ? null : new(doc);
    public static implicit operator GeminiSchemaResponseFormat(JsonElement? doc) => doc == null ? null : new(doc);
}
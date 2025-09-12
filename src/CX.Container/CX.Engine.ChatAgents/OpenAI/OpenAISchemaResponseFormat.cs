using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using CX.Engine.ChatAgents.OpenAI.Schemas;

namespace CX.Engine.ChatAgents.OpenAI;

public class OpenAISchemaResponseFormat : SchemaResponseFormat
{

    protected OpenAISchemaResponseFormat()
    {}
    
    public OpenAISchemaResponseFormat(SchemaBase typedSchema) : base(typedSchema)
    {
    }

    public OpenAISchemaResponseFormat(JsonElement? rawSchema) : base(rawSchema)
    {
    }

    public OpenAISchemaResponseFormat(JsonNode rawSchemaNode) : base(rawSchemaNode)
    {
    }

    public override void Serialize(Utf8JsonWriter jw)
    {
        jw.WriteStartObject();
        jw.WriteString("type"u8, "json_schema"u8);
        jw.WritePropertyName("json_schema"u8);
        if (TypedSchema != null)
            TypedSchema.Serialize(jw);
        else if (RawSchemaNode != null)
            RawSchemaNode.WriteTo(jw);
        else
            RawSchema!.Value.WriteTo(jw);
        jw.WriteEndObject();
    }

    public override string ToString()
    {
        var ms = new MemoryStream();
        var jw = new Utf8JsonWriter(ms);
        Serialize(jw);
        jw.Flush();
        return Encoding.UTF8.GetString(ms.ToArray());
    }

    public static implicit operator OpenAISchemaResponseFormat(JsonNode doc) => doc == null ? null : new(doc);
    public static implicit operator OpenAISchemaResponseFormat(JsonElement? doc) => doc == null ? null : new(doc);
}
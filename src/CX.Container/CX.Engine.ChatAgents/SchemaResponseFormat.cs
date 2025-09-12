using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace CX.Engine.ChatAgents;

public abstract class SchemaResponseFormat
{
    [JsonInclude]
    public SchemaBase TypedSchema;
    [JsonInclude]
    public JsonElement? RawSchema;
    [JsonInclude]
    public JsonNode RawSchemaNode;
    public string GeminiSchemaPath { get; set; }

    protected SchemaResponseFormat()
    {}

    protected SchemaResponseFormat(SchemaBase typedSchema)
    {
        TypedSchema = typedSchema ?? throw new ArgumentNullException(nameof(typedSchema));
    }


    protected SchemaResponseFormat(JsonElement? rawSchema)
    {
        RawSchema = rawSchema ?? throw new ArgumentNullException(nameof(rawSchema));
    }


    protected SchemaResponseFormat(JsonNode rawSchemaNode)
    {
        RawSchemaNode = rawSchemaNode ?? throw new ArgumentNullException(nameof(rawSchemaNode));
    }

    public abstract void Serialize(Utf8JsonWriter jw);

    public override string ToString()
    {
        var ms = new MemoryStream();
        var jw = new Utf8JsonWriter(ms);
        Serialize(jw);
        jw.Flush();
        return Encoding.UTF8.GetString(ms.ToArray());
    }
}
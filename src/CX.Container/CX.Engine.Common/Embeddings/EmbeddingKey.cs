using System.Text.Json;
using CX.Engine.Common.Json;

namespace CX.Engine.Common.Embeddings;

public readonly record struct EmbeddingKey(string Model, string Content) : ISerializeJson
{
    public readonly string Model = Model;
    public readonly string Content = Content;

    public void Serialize(Utf8JsonWriter jw)
    {
        jw.WriteStartObject();
        jw.WritePropertyName("model");
        jw.WriteStringValue(Model);
        jw.WritePropertyName("content");
        jw.WriteStringValue(Content);
        jw.WriteEndObject();
    }
    
    public void Serialize(BinaryWriter bw)
    {
        bw.Write(Model);
        bw.Write(Content);
    }
    
    public static EmbeddingKey FromBinaryReader(BinaryReader br)
    {
        var model = br.ReadString();
        var content = br.ReadString();
        return new(model, content);
    }
}
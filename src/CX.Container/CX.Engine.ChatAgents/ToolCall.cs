using System.Text.Json;
using System.Text.Json.Serialization;
using CX.Engine.Common;

namespace CX.Engine.ChatAgents;

public sealed class ToolCall
{
    [JsonInclude]
    public string Id = null!;
    [JsonInclude]
    public string Name = null!;
    [JsonInclude]
    public string Arguments;

    public ToolCall()
    {
    }

    public ToolCall(ChatLoadContext clc)
    {
        Deserialize(clc);
    }

    public void Deserialize(ChatLoadContext clc)
    {
        var br = clc.Br;
        Id = br.ReadString();
        Name = br.ReadString();
        Arguments = br.ReadStringNullable();
    }

    public void Serialize(BinaryWriter bw)
    {
        bw.Write(Id ?? "");
        bw.Write(Name);
        bw.WriteNullable(Arguments);
    }

    public void Serialize(Utf8JsonWriter jw)
    {
        jw.WriteStartObject();
        jw.WritePropertyName("id");
        jw.WriteStringValue(Id);
        jw.WritePropertyName("type");
        jw.WriteStringValue("function");
        jw.WritePropertyName("function");
        jw.WriteStartObject();
        jw.WritePropertyName("name");
        jw.WriteStringValue(Name);
        jw.WritePropertyName("arguments");
        jw.WriteStringValue(Arguments ?? "");
        jw.WriteEndObject();
        jw.WriteEndObject();
    }
}
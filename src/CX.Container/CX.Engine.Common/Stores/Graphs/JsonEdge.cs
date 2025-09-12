using System.Data.Common;
using CX.Engine.Common.Db;
using Newtonsoft.Json;

namespace CX.Engine.Common.Stores.Graphs;

public class JsonEdge
{
    public long? Id;
    public string Source;
    public string Target;
    public Dictionary<string, object> Meta;

    public string EdgeType
    {
        get => Meta?.GetValueOrDefault("type") as string ?? "default";
        set
        {
            Meta ??= new();
            Meta["type"] = value;
        }
    }

    public JsonEdge()
    {
    }
    
    public JsonEdge(string source, string target, Dictionary<string, object> meta = null)
    { 
        Source = source;
        Target = target;
        Meta = meta;
    }
    
    public static JsonEdge Map(DbDataReader reader) =>
        new()
        {
            Id = reader.Get<long>("id"),
            Source = reader.Get<string>("source"),
            Target = reader.Get<string>("target"),
            Meta = JsonConvert.DeserializeObject<Dictionary<string, object>>(reader.Get<string>("meta"))
        };

    public override string ToString() => $"{Source} ---({EdgeType})---> {Target}";
}
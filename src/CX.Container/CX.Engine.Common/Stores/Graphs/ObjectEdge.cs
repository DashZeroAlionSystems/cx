namespace CX.Engine.Common.Stores.Graphs;

public class ObjectEdge
{
    public long? Id;
    public object Source;
    public object Target;
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

    public override string ToString() => $"{Source} ---({EdgeType})---> {Target}";
    
    public object OtherEnd(object obj)
    {
        if (obj == Source)
            return Target;
        if (obj == Target)
            return Source;
        
        throw new InvalidOperationException("Object is not part of the edge");
    }
}
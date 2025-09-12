using CX.Engine.Common.Stores.Json;

namespace CX.Engine.Common.Stores.Graphs;

public static class ObjectEdgeExt
{
    public static List<string> OtherEdgeStoreKeys(this List<ObjectEdge> edge, object focalObject)
    {
        return edge
            .Where(e => e.Source == focalObject || e.Target == focalObject)
            .Select(e => e.Source == focalObject ? e.Target : e.Source)
            .Select(o => o is IStoreObject so ? so.StoreKey : o?.ToString())
            .ToList();
    }
    
    public static IEnumerable<ObjectEdge> OutboundOfType<T>(this List<ObjectEdge> edges, object focalObject)
    {
        return edges.Where(e => e.Source == focalObject && e.Target is T && e.Source != e.Target);
    }
    
    public static IEnumerable<ObjectEdge> InboundOfType<T>(this List<ObjectEdge> edges, object focalObject)
    {
        return edges.Where(e => e.Target == focalObject && e.Source is T && e.Source != e.Target);
    }
}
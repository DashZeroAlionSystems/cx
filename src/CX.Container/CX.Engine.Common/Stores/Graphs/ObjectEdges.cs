using JetBrains.Annotations;

namespace CX.Engine.Common.Stores.Graphs;

public class ObjectEdges
{
    public object FocalObject;
    public List<ObjectEdge> Edges;
    
    public ObjectEdges([NotNull] object focalObject, [NotNull] List<ObjectEdge> edges)
    {
        FocalObject = focalObject ?? throw new ArgumentNullException(nameof(focalObject));
        Edges = edges ?? throw new ArgumentNullException(nameof(edges));
    }
    
    public List<string> OtherEdgeStoreKeys() => Edges.OtherEdgeStoreKeys(FocalObject);
    public IEnumerable<ObjectEdge> OutboundOfType<T>() => Edges.OutboundOfType<T>(FocalObject);
    public IEnumerable<ObjectEdge> InboundOfType<T>() => Edges.InboundOfType<T>(FocalObject);

}
namespace CX.Engine.Archives.Pinecone;

[UniqueComponent]
public class NamespaceFilter : IChunkArchiveRetrievalRequestComponent
{
    public string Namespace;
    
    public NamespaceFilter(string ns)
    {
        Namespace = ns;
    }
}

public static class NamespaceFilterExt
{
    public static string GetNamespaceFilter(this ChunkArchiveRetrievalRequest req)
    {
        var ns = req.Components.GetValueOrDefault<NamespaceFilter>();
        if (ns == null)
            return null;
        return ns.Namespace.NullIfWhiteSpace();
    }
    
    public static void SetNamespaceFilter(this ChunkArchiveRetrievalRequest req, string ns)
    {
        if (req.Components.TryGet<NamespaceFilter>(out var filter))
        {
            filter.Namespace = ns;
            return;
        }
        else
        {
            req.Components.Add(new NamespaceFilter(ns));
            return;
        }
    }
}
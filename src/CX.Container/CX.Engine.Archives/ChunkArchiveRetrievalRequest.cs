namespace CX.Engine.Archives;

/// <summary>
/// NB: extend this class using Components, not inheritance
/// </summary>
public sealed class ChunkArchiveRetrievalRequest 
{
    public string QueryString;
    public double MinSimilarity;
    public int CutoffTokens;
    public int? MaxChunks;
    public Components<IChunkArchiveRetrievalRequestComponent> Components = new();

    public ChunkArchiveRetrievalRequest()
    {
    }

    public ChunkArchiveRetrievalRequest(string queryString, double minSimilarity, int cutoffTokens, int? maxChunks = null)
    {
        QueryString = queryString;
        MinSimilarity = minSimilarity;
        CutoffTokens = cutoffTokens;
        MaxChunks = maxChunks;
    }
    
    public static implicit operator ChunkArchiveRetrievalRequest((string queryString, double minSimilarity, int cutoffTokens, int? maxChunks) values) =>
        new(values.queryString, values.minSimilarity, values.cutoffTokens, values.maxChunks);
    
    public static implicit operator ChunkArchiveRetrievalRequest((string queryString, double minSimilarity, int cutoffTokens) values) =>
        new(values.queryString, values.minSimilarity, values.cutoffTokens);
}    

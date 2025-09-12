using CX.Engine.TextProcessors.Splitters;

namespace CX.Engine.Archives;

public abstract class BaseChunkArchive : IChunkArchive
{
    public abstract Task ImportAsync(TextChunk chunk);
    public abstract Task ClearAsync();
    public abstract Task<List<ArchiveMatch>> RetrieveAsync(ChunkArchiveRetrievalRequest request);

    public static List<ArchiveMatch> OrderAndApplyTokenCutoffAndMaxChunks(List<ArchiveMatch> inputs, int tokenLimit, int? maxChunks)
    {
        //Sort the result by Score descending
        inputs.Sort((a, b) => b.Score.CompareTo(a.Score));

        //Apply a token cut-off and max chunks
        var res = new List<ArchiveMatch>();
        var tokens = 0;
        foreach (var match in inputs)
        {
            if (tokens + match.Chunk.EstTokens > tokenLimit)
                break;

            tokens += match.Chunk.EstTokens;
            res.Add(match);
            
            if (maxChunks.HasValue && res.Count == maxChunks)
                break;
        }

        return res;        
    }
    
    public abstract Task RemoveDocumentAsync(Guid documentId);
    
    public abstract Task ImportAsync(Guid documentId, List<TextChunk> chunks);
}
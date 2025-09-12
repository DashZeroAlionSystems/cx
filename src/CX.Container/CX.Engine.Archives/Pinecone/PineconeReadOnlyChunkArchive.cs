using CX.Engine.Common.Embeddings;
using CX.Engine.TextProcessors.Splitters;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CX.Engine.Archives.Pinecone;

public class PineconeReadOnlyChunkArchive : PineconeBaseChunkArchive
{
    public PineconeReadOnlyChunkArchive(string name, IOptionsMonitor<PineconeOptions> options, EmbeddingCache embeddingCache, IServiceProvider sp, ILogger logger) : base(name, options, embeddingCache, sp, logger)
    {
    }
    
    public override Task ImportAsync(TextChunk chunk)
    {
        throw new NotSupportedException();
    }

    public override Task ClearAsync()
    {
        throw new NotSupportedException();
    }

    public override Task RemoveDocumentAsync(Guid documentId)
    {
        throw new NotSupportedException();
    }
    
    public override Task ImportAsync(Guid documentId, List<TextChunk> chunks)
    {
        throw new NotSupportedException();
    }
}

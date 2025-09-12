using CX.Engine.TextProcessors.Splitters;

namespace CX.Engine.Archives;

public interface IChunkArchive : IArchive
{
    Task ImportAsync(TextChunk chunk);
    Task<List<ArchiveMatch>> RetrieveAsync(ChunkArchiveRetrievalRequest request);
    Task ImportAsync(Guid documentId, List<TextChunk> chunks);
}
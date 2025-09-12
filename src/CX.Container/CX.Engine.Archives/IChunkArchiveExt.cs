using CX.Engine.TextProcessors.Splitters;

namespace CX.Engine.Archives;

public static class IChunkArchiveExt
{
    public static async Task RegisterAsync(this IChunkArchive chunkArchive, string s, CXMeta metadata)
    {
        var chunk = new TextChunk(s, metadata ?? new());
        await chunkArchive.ImportAsync(chunk);
    }
    
    public static Task RegisterAsync(this IChunkArchive chunkArchive, params string[] ss) => Task.WhenAll(ss.Select(s => chunkArchive.ImportAsync(s)));
    
    public static Task RegisterAsync(this IChunkArchive chunkArchive, List<TextChunk> chunks) => Task.WhenAll(chunks.Select(chunkArchive.ImportAsync));

    public static async Task ClearAndRegisterAsync(this IChunkArchive chunkArchive, Guid documentId, List<TextChunk> chunks)
    {
        await chunkArchive.ClearAsync();
        await chunkArchive.ImportAsync(documentId, chunks);
    }
}
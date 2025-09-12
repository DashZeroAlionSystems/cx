using CX.Engine.Common.Embeddings;
using CX.Engine.Common.Tracing;
using CX.Engine.TextProcessors.Splitters;

namespace CX.Engine.Archives.InMemory;

public class InMemoryChunkArchive : BaseChunkArchive
{
    public readonly List<Entry> Entries = new();
    public readonly SemaphoreSlim SlimLock = new(1, 1);
    public readonly InMemoryArchiveOptions Options;

    private readonly EmbeddingCache _embeddingCache;

    public InMemoryChunkArchive(EmbeddingCache embeddingCache, InMemoryArchiveOptions options)
    {
        _embeddingCache = embeddingCache ?? throw new ArgumentNullException(nameof(embeddingCache));
        Options = options ?? throw new ArgumentNullException(nameof(options));
        Options.Validate();
    }

    public override async Task ImportAsync(TextChunk chunk)
    {
        var res = await _embeddingCache.GetAsync(Options.EmbeddingModel, chunk.GetSurroundingContextString());

        using var _ = await SlimLock.UseAsync();
        Entries.Add(new(chunk, res));
    }

    public override async Task ClearAsync()
    {
        using var _ = await SlimLock.UseAsync();
        Entries.Clear();
    }

    public override async Task<List<ArchiveMatch>> RetrieveAsync(ChunkArchiveRetrievalRequest req)
    {
        var searchEmbeds = await _embeddingCache.GetAsync(Options.EmbeddingModel, req.QueryString);

        var res = new List<ArchiveMatch>();

        await CXTrace.Current.SpanFor(CXTrace.Section_RetrieveMatches,
            new
            {
                MinSimilarity = req.MinSimilarity,
                CutoffTokens = req.CutoffTokens,
                MaxChunks = req.MaxChunks
            }).ExecuteAsync(async span =>
        {
            using var _ = await SlimLock.UseAsync();
            foreach (var entry in Entries)
            {
                var similarity = searchEmbeds.GetCosineSimilarity(entry.Embedding);
                if (similarity > req.MinSimilarity)
                {
                    var atts = entry.Chunk.Metadata.GetAttachments(false);
                    if (atts != null)
                        foreach (var att in atts)
                            att.Context = entry.Chunk.GetAttachmentContextString();

                    res.Add(new(entry.Chunk, similarity));
                }

                res = OrderAndApplyTokenCutoffAndMaxChunks(res, req.CutoffTokens, req.MaxChunks);
                span.Output = res;
            }
        });

        return res;
    }

    public override async Task RemoveDocumentAsync(Guid documentId)
    {
        using var _ = await SlimLock.UseAsync();
        Entries.RemoveAllFast(e => e.Chunk.Metadata.DocumentId == documentId);
    }

    /// <summary>
    /// Retrieves a random TextChunk asynchronously from the InMemory1Archive.
    /// </summary>
    /// <returns>The randomly selected TextChunk.</returns>
    public async Task<TextChunk> GetRandomChunkAsync()
    {
        using var _ = await SlimLock.UseAsync();
        
        if (Entries.Count == 0)
            throw new InvalidOperationException("No entries to select from.");

        var randomIndex = Random.Shared.Next(Entries.Count);
        return Entries[randomIndex].Chunk;
    }

    public override Task ImportAsync(Guid documentId, List<TextChunk> chunks) => this.RegisterAsync(chunks);
}
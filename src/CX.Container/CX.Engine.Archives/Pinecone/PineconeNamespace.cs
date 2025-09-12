using CX.Engine.TextProcessors.Splitters;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CX.Engine.Archives.Pinecone;

public class PineconeNamespace : IDisposable, IChunkArchive
{
    private StateSnapshot _snapshot;
    private readonly TaskCompletionSource _tcsInitialized = new();
    private readonly IDisposable _optionsChangeDisposable;
    private readonly ILogger _logger;
    private readonly IServiceProvider _sp;

    private class StateSnapshot
    {
        public PineconeNamespaceOptions Options;
        public PineconeBaseChunkArchive ChunkArchive;
    }

    private void ApplyOptions(PineconeNamespaceOptions options)
    {
        try
        {
            var ss = new StateSnapshot();
            ss.Options = options;
            var archive = _sp.GetRequiredNamedService<IChunkArchive>(options.PineconeArchive);
            if (archive is not PineconeBaseChunkArchive pba)
                throw new InvalidOperationException($"Archive {options.PineconeArchive} is not a Pinecone archive");
            ss.ChunkArchive = pba;

            _snapshot = ss;
            _tcsInitialized.TrySetResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply new options - they will be ignored.");
        }
    }

    public PineconeNamespace(IOptionsMonitor<PineconeNamespaceOptions> options, IServiceProvider sp, ILogger logger)
    {
        _snapshot = new();
        _snapshot.Options = options.CurrentValue;
        
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _sp = sp ?? throw new ArgumentNullException(nameof(sp));
        
        _optionsChangeDisposable = options.Snapshot(() => _snapshot?.Options, ApplyOptions, logger, sp);
    }

    public void Dispose()
    {
        _optionsChangeDisposable?.Dispose();
    }

    public async Task ImportAsync(TextChunk chunk)
    {
        await _tcsInitialized.Task;
        await _snapshot.ChunkArchive.ImportAsync(chunk);
    }

    public async Task ClearAsync()
    {
        await _tcsInitialized.Task;
        var ss = _snapshot;
        if (ss.ChunkArchive is not PineconeChunkArchive pa)
            throw new InvalidOperationException("Archive is not a writeable Pinecone archive");
        
        await pa.ClearAsync(ss.Options.Namespace);
    }

    public async Task<List<ArchiveMatch>> RetrieveAsync(ChunkArchiveRetrievalRequest req)
    {
        await _tcsInitialized.Task;
        var ss = _snapshot;

        req.SetNamespaceFilter(ss.Options.Namespace);
        
        return await ss.ChunkArchive.RetrieveAsync(req);
    }

    public async Task RemoveDocumentAsync(Guid documentId)
    {
        await _tcsInitialized.Task;
        await _snapshot.ChunkArchive.RemoveDocumentAsync(documentId);
    }

    public async Task ImportAsync(Guid documentId, List<TextChunk> chunks)
    {
        await _tcsInitialized.Task;
        var archive = _snapshot.ChunkArchive;
        
        if (archive is not PineconeChunkArchive pa)
            throw new InvalidOperationException("Archive is not a writeable Pinecone archive");
        
        await pa.RegisterAsync(documentId, chunks, _snapshot.Options.Namespace); 
    }
}
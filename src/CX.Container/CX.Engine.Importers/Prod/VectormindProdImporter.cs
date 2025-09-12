using CX.Engine.Archives;
using CX.Engine.Common;
using CX.Engine.Common.Tracing;
using CX.Engine.Common.Tracing.Langfuse;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CX.Engine.Importing.Prod;

public class VectormindProdImporter
{
    private readonly ILogger<VectormindProdImporter> _logger;
    private readonly LangfuseService _langfuse;
    public event EventHandler DocumentImported;

    private readonly VectormindProdImporterOptions _options;
    private readonly IChunkArchive _chunkArchive;
    private readonly SemaphoreSlim _semaphoreSlim;
    private readonly ProdS3Helper _s3Helper;
    private readonly VectorLinkImporter _importer;
    private readonly ProdRepo _repo;

    public VectormindProdImporter(IOptions<VectormindProdImporterOptions> options, IServiceProvider sp,
        ILogger<VectormindProdImporter> logger, LangfuseService langfuse,
        VectorLinkImporter importer)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _langfuse = langfuse ?? throw new ArgumentNullException(nameof(langfuse));
        _importer = importer ?? throw new ArgumentNullException(nameof(importer));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _options.Validate();
        _chunkArchive = sp.GetRequiredNamedService<IChunkArchive>(_options.ArchiveName);
        _repo = sp.GetRequiredNamedService<ProdRepo>(_options.ProdRepoName);
        _s3Helper = sp.GetRequiredNamedService<ProdS3Helper>(_options.ProdS3HelperName);
        _semaphoreSlim = new(_options.MaxConcurrency!.Value, _options.MaxConcurrency.Value);
    }

    public async Task ImportAsync()
    {
        _logger.LogInformation("Retrieving from database {dbName}...", _repo.Options.PostgreSQLClientName);

        var docs = await _repo.GetTrainedSourceDocumentsAsync();

        if (docs.Any(doc => doc == null))
            throw new InvalidOperationException($"{nameof(docs)} contains nulls");

        foreach (var doc in docs.Where(doc => string.IsNullOrWhiteSpace(doc.DisplayName)))
            doc.DisplayName = doc.Id.ToString();

        var citations = await _repo.GetAllCitationsAsync();

        foreach (var citation in citations.Where(cit => cit.Name == null))
            citation.Name = citation.Id.ToString();

        var docLookup = docs.Where(doc => doc != null).ToDictionary(doc => doc.Id);

        foreach (var citation in citations)
        {
            if (!docLookup.TryGetValue(citation.SourceDocumentId, out var doc))
                continue;

            (doc.Citations ??= new()).Add(citation);
        }

        if (_options.OnlyImportDocumentsWithAttachments)
            docs.RemoveAll(doc => (doc.Citations?.Count ?? 0) < 1);

        if (_options.ClearArchive)
        {
            _logger.LogInformation("Clearing archive...");
            await _chunkArchive.ClearAsync();
        }

        _logger.LogInformation("Importing {count} documents and their attachments into {archiveName}...", docs.Count, _options.ArchiveName);

        var busyFiles = 0;
        var tcsAllQueued = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var tcsAllDone = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        async Task HandleFileAsync(ProdRepo.SourceDocument doc)
        {
            if (_options.SingleDocumentLogLevel >= 1)
                _logger.LogInformation($"Importing source document {doc.Id} {doc.DisplayName}...");

            await CXTrace.GetImportTrace(_langfuse)
                .WithName($"Importing source document {doc.Id} {doc.DisplayName}")
                .WithInput(new
                {
                    Id = doc.Id,
                    DisplayName = doc.DisplayName,
                    Citations = doc.Citations?.Count ?? 0
                }).ExecuteAsync(async _ =>
                {
                    try
                    {
                        if (doc.AwsFileName == null)
                            throw new InvalidOperationException("Filter missing");

                        if (_options.SingleDocumentLogLevel >= 2)
                            _logger.LogInformation("Downloading document content from AWS...");

                        var content = await _s3Helper.GetObjectAsync(doc.AwsFileName!);

                        if (content == null)
                        {
                            _logger.LogWarning(
                                $"Document {doc.Id} {doc.DisplayName} with filename {doc.AwsFileName} has no downloadable content");
                            return;
                        }

                        var job = new VectorLinkImportJob
                        {
                            Description = null,
                            Attachments = doc.Citations?.Select(c => new AttachmentInfo
                            {
                                CitationId = c.Id,
                                FileName = c.Name,
                                FileUrl = c.Url,
                                DoGetContentStreamAsync = () => Task.FromResult<Stream>(new MemoryStream(c.Content!))
                            }).ToList(),
                            DocumentId = doc.Id,
                            SourceDocumentDisplayName = doc.DisplayName,
                            DocumentContent = await content.CopyToMemoryStreamAsync()
                        };

                        if (_options.SingleDocumentLogLevel >= 2)
                            _logger.LogInformation("Importing document content...");

                        await _importer.ImportAsync(job);

                        try
                        {
                            DocumentImported?.Invoke(this, EventArgs.Empty);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"Processing {nameof(DocumentImported)} event");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"During import of {doc.DisplayName}");
                    }
                });

            if (_options.SingleDocumentLogLevel >= 2)
                _logger.LogInformation($"Imported source document {doc.Id} {doc.DisplayName}.");
        }

        async void QueueFile(ProdRepo.SourceDocument doc)
        {
            Interlocked.Increment(ref busyFiles);

            using (var _ = await _semaphoreSlim.UseAsync())
                await HandleFileAsync(doc);

            await tcsAllQueued.Task;

            var after = Interlocked.Decrement(ref busyFiles);

            if (after == 0)
                tcsAllDone.SetResult();
        }

        foreach (var doc in docs)
        {
            if (doc == null)
                throw new InvalidOperationException($"{nameof(doc)} == null");

            QueueFile(doc);
        }

        tcsAllQueued.SetResult();

        if (docs.Count != 0)
            await tcsAllDone.Task;

        _logger.LogInformation("{totalChunks} chunks imported.", _importer.TotalChunksImported);
    }
}
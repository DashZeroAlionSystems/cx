using CX.Engine.Archives;
using CX.Engine.Common;
using CX.Engine.Common.DistributedLocks;
using CX.Engine.Common.Meta;
using CX.Engine.Common.Stores.Binary;
using CX.Engine.Common.Stores.Json;
using CX.Engine.Common.Tracing;
using CX.Engine.Common.Tracing.Langfuse;
using CX.Engine.DocExtractors.Images;
using CX.Engine.DocExtractors.Text;
using CX.Engine.Importing.Prod;
using CX.Engine.TextProcessors;
using CX.Engine.TextProcessors.Splitters;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CX.Engine.Importing;

[PublicAPI]
public class VectorLinkImporter : IDisposable
{
    private readonly LangfuseService _langfuseService;
    private readonly IServiceProvider _sp;
    private readonly ILogger<VectorLinkImporter> _logger;
    private readonly DocImageExtraction _docImageExtraction;
    private readonly DocTextExtractionRouter _docTextExtractionRouter;
    private readonly DistributedLockService _distributedLockService;
    private readonly SemaphoreSlim _maxConcurrency;
    private readonly IDisposable _optionsMonitorDisposable;
    private SnapshotClass _snapshot;
    public SnapshotClass Snapshot => _snapshot;

    public Func<(HandleFileContext Context, string Errror), ValueTask> LogExtractionErrorAsync;
    public Func<(HandleFileContext Context, Exception Exception), ValueTask> LogImageExtractionExceptionAsync;
    private long _totalChunksImported;
    public long TotalChunksImported => _totalChunksImported;

    public class SnapshotClass
    {
        public VectorLinkImporterOptions Options;
        public IJsonStore AttachmentTracker;
        public ProdRepo ProdRepo;
        
        public Func<(HandleFileContext Context, string Text), ValueTask> UpdateExtractedTextAsync =
            _ => ValueTask.CompletedTask;

        public Func<(HandleFileContext Context, string Text), ValueTask> UpdateProcessedTextAsync =
            _ => ValueTask.CompletedTask;

        public Func<(HandleFileContext Context, string Text), ValueTask> UpdateImportWarningsAsync =
            _ => ValueTask.CompletedTask;

    }

    public class HandleFileContext
    {
        public readonly SnapshotClass ImporterSnapshot;
        public readonly VectorLinkImportJob Job;
        public readonly Guid FileId;
        public readonly bool IsAttachment;
        public string Warnings;

        public async Task LogWarningAsync(string warning)
        {
            Warnings += $"\n{warning}";
            await ImporterSnapshot.UpdateImportWarningsAsync((this, Warnings));
        }

        public HandleFileContext(SnapshotClass snapshot, VectorLinkImportJob job, Guid fileId, bool isAttachment)
        {
            ImporterSnapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
            Job = job ?? throw new ArgumentNullException(nameof(job));
            FileId = fileId;
            IsAttachment = isAttachment;
        }
    }

    private void SetSnapshot(VectorLinkImporterOptions options)
    {
        var ss = new SnapshotClass();
        ss.Options = options;
        ss.AttachmentTracker = _sp.GetRequiredNamedService<IJsonStore>(options.AttachmentTrackerName);
        ss.ProdRepo = !string.IsNullOrWhiteSpace(options.ProdRepoName)
            ? _sp.GetRequiredNamedService<ProdRepo>(options.ProdRepoName)
            : null;
        
        if (ss.ProdRepo != null)
        {
            ss.UpdateExtractedTextAsync = async args =>
            {
                if (!args.Context.IsAttachment)
                    await ss.ProdRepo.SetDocumentOcrTextAsync(args.Context.FileId, args.Text);
                else
                    await ss.ProdRepo.SetCitationOcrTextAsync(args.Context.FileId, args.Text);
            };

            ss.UpdateProcessedTextAsync = async args =>
            {
                if (!args.Context.IsAttachment)
                    await ss.ProdRepo.SetDocumentDecoratorTextAsync(args.Context.FileId, args.Text);
                else
                    await ss.ProdRepo.SetCitationDecoratorTextAsync(args.Context.FileId, args.Text);
            };

            ss.UpdateImportWarningsAsync = async args =>
            {
                if (!args.Context.IsAttachment)
                    await ss.ProdRepo.SetDocumentImportWarningsTextAsync(args.Context.FileId, args.Text);
                else
                    await ss.ProdRepo.SetCitationImportWarningsAsync(args.Context.FileId, args.Text);
            };
        }
        
        _snapshot = ss;
    }
    
    public VectorLinkImporter(IServiceProvider sp,
        IOptionsMonitor<VectorLinkImporterOptions> monitor,
        ILogger<VectorLinkImporter> logger,
        DocImageExtraction docImageExtraction,
        DocTextExtractionRouter docTextExtractionRouter,
        DistributedLockService distributedLockService,
        LangfuseService langfuseService = null)
    {
        _langfuseService = langfuseService;
        _sp = sp ?? throw new ArgumentNullException(nameof(sp));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _docImageExtraction = docImageExtraction ?? throw new ArgumentNullException(nameof(docImageExtraction));
        _docTextExtractionRouter =
            docTextExtractionRouter ?? throw new ArgumentNullException(nameof(docTextExtractionRouter));
        _distributedLockService =
            distributedLockService ?? throw new ArgumentNullException(nameof(distributedLockService));
        _optionsMonitorDisposable = monitor.Snapshot(() => _snapshot?.Options, SetSnapshot, logger, sp); 

        LogImageExtractionExceptionAsync = async args =>
        {
            //We specifically log these to get a better idea of what failures to handle here - none are known at this stage.
            _logger.LogError(args.Exception,
                $"During image extraction of {args.Context.Job.SourceDocumentDisplayName} ({args.Context.Job.DocumentId})");
            await args.Context.LogWarningAsync($"Image extraction error: {args.Exception.Message}");
        };
        LogExtractionErrorAsync = async args =>
        {
            //These exceptions are well known and don't need developer attention.
            await args.Context.LogWarningAsync($"Text extraction error: {args.Errror}");
        };
        _maxConcurrency = new(1, 1);
    }

    public async Task ImportAsync(VectorLinkImportJob job, CXTrace trace = null)
    {
        if (job == null)
            throw new ArgumentNullException(nameof(job));

        job.Validate();

        var ss = _snapshot;

        await (trace ?? CXTrace.GetImportTrace(_langfuseService)
                .WithName($"Import document {job.DocumentId}")
                .WithInput(new
                {
                    Description = job.Description,
                    DocumentId = job.DocumentId.ToString(),
                    Attachments = job.Attachments,
                    Archive = job.Archive ?? ss.Options.ArchiveName
                }))
            .ExecuteAsync(async _ =>
            {
                var archive = _sp.GetRequiredNamedService<IChunkArchive>(job.Archive ?? ss.Options.ArchiveName);

                await using var __ =
                    await _distributedLockService.UseAsync<VectorLinkImporter>(job.DocumentId.ToString());

                //Make sure we have a clean slate before we start to (re)-import potentially changed content.
                await DeleteAsync(job.DocumentId, false);

                async Task HandleFileAsync(Guid fileId, string fileName, Stream content, DocumentMeta meta,
                    bool isAttachment)
                {
                    await CXTrace.Current.SpanFor(CXTrace.Section_Import, new
                    {
                        FileId = fileId,
                        FileName = fileName,
                        IsAttachment = isAttachment
                    }).ExecuteAsync(async _ =>
                    {
                        var fileContext = new HandleFileContext(ss, job, fileId, isAttachment);

                        if (meta.Id == null)
                            throw new InvalidOperationException($"{nameof(meta)}.{nameof(meta.Id)} is required");

                        using var ___ = await _maxConcurrency.UseAsync();

                        List<string> images = null;
                        IBinaryStore imageStore = null;

                        var extractImages = job.ExtractImages ?? ss.Options.ExtractImages;
                        var gotImages = false;
                        if (extractImages)
                        {
                            try
                            {
                                imageStore = _docImageExtraction.PDFToJpg.ImageStore;
                                images = await _docImageExtraction.ExtractImagesAsync(fileId,
                                    content);
                                gotImages = images?.Count > 0;
                            }
                            catch (Exception ex)
                            {
                                await LogImageExtractionExceptionAsync((fileContext, ex));
                            }
                        }

                        var text = await _docTextExtractionRouter.ExtractToTextAsync(fileName,
                            content,
                            meta,
                            imageStore,
                            images,
                            job.PreferImageTextExtraction ?? ss.Options.PreferImageTextExtraction);

                        if (meta.ExtractionErrors != null)
                            foreach (var err in meta.ExtractionErrors)
                                await LogExtractionErrorAsync((fileContext, err));

                        await ss.UpdateImportWarningsAsync((fileContext, ""));
                        await ss.UpdateExtractedTextAsync((fileContext, text));

                        text = await TextProcessingDI.ProcessAsync(text, _sp, ss.Options.DocumentProcessors);

                        await ss.UpdateProcessedTextAsync((fileContext, text));

                        var chunker = _sp.GetRequiredService<LineSplitter>();
                        var req = new LineSplitterRequest(text, meta)
                        {
                            AttachPageImages = gotImages && (job.AttachPageImages ?? ss.Options.DefaultAttachPageImages ?? true) 
                        };

                        if ((job.AttachToSelf ?? ss.Options.AttachToSelf) || isAttachment)
                            (meta.Attachments ??= new()).Add(new()
                            {
                                CitationId = fileId,
                                FileName = fileName,
                                FileUrl = isAttachment ? $"/api/citations/{fileId}" : $"/api/documents/{fileId}",
                                DoGetContentStreamAsync = () => Task.FromResult(content)!,
                            });

                        var chunks = await chunker.ChunkAsync(req);
                        Interlocked.Add(ref _totalChunksImported, chunks.Count);
                        await archive.ImportAsync(meta.Id.Value, chunks);
                    });
                }

                var meta = new DocumentMeta
                {
                    Id = job.DocumentId,
                    Description = job.Description,
                    SourceDocument = job.SourceDocumentDisplayName,
                    SourceDocumentGroup = job.SourceDocumentDisplayName,
                    Tags = job.Tags
                };

                if (job.Attachments?.Count > 0)
                {
                    meta.AddAttachments(job.Attachments);
                }

                var tasks = new List<Task>();
                tasks.Add(HandleFileAsync(job.DocumentId, job.SourceDocumentDisplayName, job.DocumentContent, meta,
                    false));

                if ((job.TrainCitations ?? ss.Options.TrainCitations) && job.Attachments != null)
                {
                    foreach (var att in job.Attachments)
                    {
                        if (string.IsNullOrWhiteSpace(att.FileName))
                            throw new InvalidOperationException($"Attachment missing {nameof(att.FileName)}");

                        if (att.DoGetContentStreamAsync == null)
                            throw new InvalidOperationException(
                                $"Attachment missing {nameof(att.DoGetContentStreamAsync)}");

                        if (!att.CitationId.HasValue)
                            throw new InvalidOperationException($"Attachment missing {nameof(att.CitationId)}");
                    }

                    await ss.AttachmentTracker.SetAsync(job.DocumentId.ToString(),
                        job.Attachments.Select(a => a.CitationId).ToList());
                    
                    foreach (var att in job.Attachments)
                    {
                        var content = await att.DoGetContentStreamAsync!();

                        if (content == null)
                            continue;

                        var attMeta = new DocumentMeta
                        {
                            Id = att.CitationId,
                            SourceDocument = att.FileName,
                            SourceDocumentGroup = job.SourceDocumentDisplayName,
                            Tags = job.Tags
                        };

                        tasks.Add(HandleFileAsync(att.CitationId!.Value, att.FileName!, content, attMeta, true));
                    }
                }

                // Wait for all tasks to complete (main document + all attachments)
                _logger.LogInformation("Waiting for all import tasks to complete - DocumentId: {DocumentId}, TaskCount: {TaskCount}", 
                    job.DocumentId, tasks.Count);
                
                try
                {
                    await Task.WhenAll(tasks);
                    _logger.LogInformation("All import tasks completed successfully - DocumentId: {DocumentId}", job.DocumentId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "One or more import tasks failed - DocumentId: {DocumentId}", job.DocumentId);
                    throw;
                }
            });
    }

    public async Task DeleteAsync(Guid documentId, bool acquireLock = true)
    {
        var sDocumentId = documentId.ToString();
        var ss = _snapshot;
        
        var archive = _sp.GetRequiredNamedService<IChunkArchive>(ss.Options.ArchiveName);

        var distLock = acquireLock ? await _distributedLockService.UseAsync<VectorLinkImporter>(sDocumentId) : null;
        try
        {
            var attachments = await ss.AttachmentTracker.GetAsync<List<Guid>>(sDocumentId);

            if (attachments != null)
                foreach (var attId in attachments)
                    await DeleteAsync(attId);

            await ss.AttachmentTracker.DeleteAsync(sDocumentId);

            await _docImageExtraction.PDFToJpg.DeleteAsync(documentId);

            await archive.RemoveDocumentAsync(documentId);
        }
        finally
        {
            if (distLock != null)
                await distLock.DisposeAsync();
        }
    }

    public void Dispose()
    {
        _optionsMonitorDisposable?.Dispose();
    }
}
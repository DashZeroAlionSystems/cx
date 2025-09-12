using System.Text.Json;
using CX.Engine.Archives;
using CX.Engine.Common;
using CX.Engine.Common.Meta;
using CX.Engine.Common.Stores.Binary;
using CX.Engine.Common.Tracing;
using CX.Engine.Common.Tracing.Langfuse;
using CX.Engine.DocExtractors.Images;
using CX.Engine.DocExtractors.Text;
using CX.Engine.FileServices;
using CX.Engine.TextProcessors.Splitters;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CX.Engine.Importers;

public class DiskImporter
{
    private readonly IChunkArchive _defaultChunkArchive;
    public IChunkArchive DefaultChunkArchive => _defaultChunkArchive;
    public readonly DiskImporterOptions Options;

    private readonly ILogger<DiskImporter> _logger;
    private readonly FileService _fileService;
    private readonly DocTextExtractionRouter _extractor;
    private readonly LineSplitter _lineSplitter;
    private readonly IServiceProvider _sp;
    private readonly LangfuseService _langfuse;
    private readonly DocImageExtraction _docImageExtraction;
    private readonly SemaphoreSlim _semaphoreSlim;

    public event EventHandler DocumentImported;
    public readonly List<Func<DiskImporter, Task>> DoneImportingAsync = new();

    public DiskImporter(IOptions<DiskImporterOptions> options, ILogger<DiskImporter> logger, FileService fileService,
        DocTextExtractionRouter extractor,
        LineSplitter lineSplitter, IServiceProvider sp, LangfuseService langfuse,
        DocImageExtraction docImageExtraction)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
        _extractor = extractor ?? throw new ArgumentNullException(nameof(extractor));
        _lineSplitter = lineSplitter ?? throw new ArgumentNullException(nameof(lineSplitter));
        _sp = sp ?? throw new ArgumentNullException(nameof(sp));
        _langfuse = langfuse ?? throw new ArgumentNullException(nameof(langfuse));
        _docImageExtraction = docImageExtraction ?? throw new ArgumentNullException(nameof(docImageExtraction));
        Options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        Options.Validate();
        _defaultChunkArchive = sp.GetRequiredNamedService<IChunkArchive>(Options.Archive);
        _semaphoreSlim = new(Options.MaxConcurrency, Options.MaxConcurrency);
    }

    public async Task ImportAsync(ImportJobMeta jobMeta, string overwriteArchiveName = null)
    {
        if (jobMeta.Skip)
            return;

        var busyFiles = 0;
        var tcsAllQueued = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var tcsAllDone = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var completedFiles = 0;
        var completedTokens = 0;
        var completedPages = 0;

        async Task HandleFileAsync(string path, string sourceDocument = null)
        {
            if (Path.GetExtension(path) == ".meta")
                return;

            await CXTrace.GetImportTrace(_langfuse)
                .WithName($"Import {Path.GetFileName(path)}")
                .WithInput(new
                {
                    Path = path,
                    SourceDocument = sourceDocument
                })
                .ExecuteAsync(async trace =>
                {
                    if (Options.LogProgressPerFile >= 2)
                      _logger.LogInformation("Importing {Path}...", path);
                    
                    try
                    {
                        var fileJobMeta = jobMeta.Clone();
                        fileJobMeta.SourceDocument = sourceDocument;

                        path = path.Replace("{Content}", Options.ContentDirectory);
                        var metaPath = path + ".meta";

                        if (File.Exists(metaPath))
                        {
                            try
                            {
                                var deserializedMeta =
                                    JsonSerializer.Deserialize<ImportJobMeta>(
                                        await File.ReadAllTextAsync(metaPath));

                                fileJobMeta.Overwrite(deserializedMeta);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Error deserializing meta file {File}", metaPath);
                                throw;
                            }
                        }

                        if (fileJobMeta.IsAttachment)
                        {
                            var cxFileInfo = new CXFileInfo();

                            if (string.IsNullOrWhiteSpace(fileJobMeta.FileId))
                                throw new InvalidOperationException($"FileId is required for attachments ({path}).");

                            cxFileInfo.FileId = fileJobMeta.FileId;
                            cxFileInfo.Path = path;
                            cxFileInfo.Description = fileJobMeta.FileDescription;
                            cxFileInfo.Name = fileJobMeta.FileName;
                            trace.Output = new
                            {
                                IsAttachment = true
                            };
                            await _fileService.AddFileMetaAsync(cxFileInfo.FileId, cxFileInfo);
                            return;
                        }

                        Guid documentId;
                        {
                            await using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                            documentId = await fs.GetSHA256GuidAsync();
                        }

                        DocumentMeta docMeta = new()
                        {
                            Id = documentId,
                            SourceDocument = fileJobMeta.SourceDocument,
                            Organization = fileJobMeta.Organization,
                            Description = fileJobMeta.FileDescription,
                            Attachments = fileJobMeta.Attachments?.ToList(),
                            Info = fileJobMeta.Info
                        };

                        List<string> images = null;
                        IBinaryStore imageStore = null;
                        var extractImages = fileJobMeta.ExtractImages ?? Options.ExtractImages;
                        if (extractImages)
                        {
                            try
                            {
                                await using var fs = new FileStream(path, FileMode.Open, FileAccess.Read,
                                    FileShare.Read);
                                imageStore = _docImageExtraction.PDFToJpg.ImageStore;
                                images = await _docImageExtraction.ExtractImagesAsync(documentId, fs);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, $"During image extraction of {path}");
                                return;
                            }
                        }

                        string content;

                        try
                        {
                            await using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                            content = await _extractor.ExtractToTextAsync(Path.GetFileName(path),
                                fs,
                                docMeta,
                                imageStore,
                                images,
                                fileJobMeta.PreferVisionTextExtractor ?? Options.PreferVisionTextExtractor);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"During text extraction of {path}");
                            return;
                        }

                        docMeta.Id = documentId;
                        if (Options.WriteExtractTextToFile) 
                            File.WriteAllText(Path.ChangeExtension(path, ".txt"), content);
                        var req = new LineSplitterRequest(content, docMeta);
                        req.AttachPageImages = extractImages;
                        
                        if (Path.GetExtension(path) == ".csv")
                            req.DocumentMeta.ColumnHeaders = (await File.ReadAllLinesAsync(path)).FirstOrDefault();
                        
                        if (fileJobMeta.Attachments?.Length > 0)
                            req.DocumentMeta.AddAttachments(fileJobMeta.Attachments);
                        
                        var chunks = await _lineSplitter.ChunkAsync(req);
                        
                        var archiveName = overwriteArchiveName.NullIfWhiteSpace() ?? jobMeta.Archive.NullIfWhiteSpace();
                        var archive = !string.IsNullOrWhiteSpace(archiveName) ? _sp.GetRequiredNamedService<IChunkArchive>(archiveName) : _defaultChunkArchive;
                        await archive.ImportAsync(documentId, chunks);

                        try
                        {
                            DocumentImported?.Invoke(this, EventArgs.Empty);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"Processing {nameof(DocumentImported)} event");
                        }

                        if (Options.LogProgressPerFile >= 1)
                        {
                            var estTokens = TokenCounter.CountTokens(content);
                            var pages = content.CountOccurences("--- PAGE");
                            Interlocked.Increment(ref completedFiles);
                            Interlocked.Add(ref completedTokens, estTokens);
                            Interlocked.Add(ref completedPages, pages);
                            _logger.LogInformation("Imported {Path} with {pages:#,##0} pages {chunks:#,##0} chunks and {estTokens:#,##0} tokens.", path, pages, chunks.Count, estTokens);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"During import of {path}");
                    }
                });
        }

        async void QueueFile(string path, string sourceDocument = null)
        {
            Interlocked.Increment(ref busyFiles);
            
            using (var _ = await _semaphoreSlim.UseAsync())
                await HandleFileAsync(path, sourceDocument);

            await tcsAllQueued.Task;
            
            var after = Interlocked.Decrement(ref busyFiles);

            if (after == 0)
                tcsAllDone.SetResult();
        }

        if (jobMeta.IsFileBased)
        {
            var filePath = jobMeta.FilePath.Replace("{Content}", Options.ContentDirectory);
            var relPath = filePath.Replace(Options.ContentDirectory, "").RemoveLeading("\\", "/")!;
            var prettyRelPath = Path.ChangeExtension(relPath, "").RemoveTrailing(".")!
                .Replace("\\", " -> ")
                .Replace("/", " -> ");

            QueueFile(jobMeta.FilePath, prettyRelPath);
        }
        else
        {
            var directory = jobMeta.DirectoryPath.Replace("{Content}", Options.ContentDirectory);
            //Recurse all files in docMeta.DirectoryPath
            var files = Directory.GetFiles(directory, jobMeta.DirectoryPattern, SearchOption.AllDirectories);
            foreach (var file in files)
            {
                var relPath = file.Replace(directory, "").RemoveLeading("\\", "/")!;
                var prettyRelPath = Path.ChangeExtension(relPath, "")
                    .Replace("\\", " -> ")
                    .Replace("/", " -> ");

                QueueFile(file, prettyRelPath);
            }
        }

        tcsAllQueued.SetResult();
        await tcsAllDone.Task;

        if (Options.LogProgressPerFile >= 1)
            _logger.LogInformation("Imported {files:#,##0} files with {estPages:#,##0} pages and {estTokens:#,##0} estimated tokens.", completedFiles, completedPages, completedTokens);

        await DoneImportingAsync.Select(ev =>
        {
            try
            {
                return ev.Invoke(this);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"During {nameof(DoneImportingAsync)} event");
                return Task.CompletedTask;
            }
        });
    }

    public async Task ImportFromOptionsAsync(string archive = null)
    {
        if (Options.ClearArchive)
        {
            _logger.LogInformation("Clearing archive...");
            await _sp.GetRequiredNamedService<IChunkArchive>(archive ?? Options.Archive).ClearAsync();
        }

        foreach (var docMeta in Options.Imports)
            await ImportAsync(docMeta, archive);
    }
}
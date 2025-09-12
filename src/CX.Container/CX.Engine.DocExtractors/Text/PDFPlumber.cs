using System.Text;
using System.Text.Json;
using CX.Engine.Common;
using CX.Engine.Common.Json;
using CX.Engine.Common.Meta;
using CX.Engine.Common.Python;
using CX.Engine.Common.Stores.Binary;
using CX.Engine.Common.Tracing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CX.Engine.DocExtractors.Text;

public class PDFPlumber : IDocumentTextExtractor
{
    private readonly ILogger<PDFPlumber> _logger;
    private readonly PDFPlumberOptions _options;
    private readonly IBinaryStore _store;
    private readonly PythonProcess _python;
    private readonly KeyedSemaphoreSlim _keyedLock = new();

    public PDFPlumber(IOptions<PDFPlumberOptions> options, IServiceProvider sp, ILogger<PDFPlumber> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _options.Validate();
        _store = sp.GetRequiredNamedService<IBinaryStore>(_options.BinaryStore);
        _python = sp.GetRequiredNamedService<PythonProcess>(_options.PythonProcess);
    }

    /// <summary>
    /// Extracts the text from a PDF stream.
    /// </summary>
    /// <param name="stream">The PDF stream.  Will be read multiple times.</param>
    /// <param name="meta">The document meta data.</param>
    public async Task<string> ExtractToTextAsync(Stream stream, DocumentMeta meta) =>
        await CXTrace.Current.SpanFor(CXTrace.Section_PDFPlumber, null)
            .ExecuteAsync(async span =>
            {
                if(meta == null)
                    throw new ArgumentNullException(nameof(meta));
                
                var cacheKey = await stream.GetSHA256Async();
                using var _ = await _keyedLock.UseAsync(cacheKey);

                var bytes = await _store.GetBytesAsync(cacheKey);
                var cached = new PDFPlumberCacheEntry(bytes);

                if (bytes != null)
                {
                    span.Output = new
                    {
                        Content = cached.Content?.Preview(2 * 1024 * 1024),
                        ExtractionErrors = cached.ExtractionErrors,
                        FromCache = true, CacheKey = cacheKey
                    };

                    if (cached.ExtractionErrors != null)
                    {
                        foreach (var err in cached.ExtractionErrors)
                            _logger.LogWarning("PDFPlumber: {Errors}", err);
                        
                        (meta.ExtractionErrors ??= []).AddRange(cached.ExtractionErrors);
                    }

                    return cached.Content ?? "";
                }

                var extractionErrors = new List<string>();
                var content = await _python.StreamToStringViaFilesAsync(_options.ScriptPath, stream, stdout =>
                {
                    if (string.IsNullOrWhiteSpace(stdout))
                        return;
                    
                    var jr = new Utf8JsonReader(Encoding.UTF8.GetBytes(stdout));
                    jr.ReadArrayOfObject(true, (ref Utf8JsonReader jr) =>
                    {
                        int? page = null;
                        string errors = null;

                        jr.ReadObjectProperties(1, false, (ref Utf8JsonReader jr, int _, string propName) =>
                        {
                            switch (propName)
                            {
                                case "pages":
                                    meta.Pages = jr.ReadInt32Value();
                                    break;
                                case "page":
                                    page = jr.ReadInt32Value();
                                    break;
                                case "errors":
                                    errors = jr.ReadStringValue();
                                    break;
                                default:
                                    jr.SkipPropertyValue();
                                    break;
                            }
                        });

                        if (errors != null)
                        {
                            _logger.LogWarning("PDFPlumber: {Errors}", errors);
                            extractionErrors.Add(page.HasValue
                                ? $"Page {page.Value}: {errors}"
                                : errors);
                        }
                    });

                    if (extractionErrors.Count > 0)
                        (meta.ExtractionErrors ??= []).AddRange(extractionErrors);
                });

                var entry = new PDFPlumberCacheEntry
                {
                    Content = content
                };
                entry.ExtractionErrors.AddRange(extractionErrors);

                span.Output = new
                    { Content = content.Preview(2 * 1024 * 1024), FromCache = false, CacheKey = cacheKey };
                await _store.SetBytesAsync(cacheKey, entry.GetBytes());
                return content;
            });
}
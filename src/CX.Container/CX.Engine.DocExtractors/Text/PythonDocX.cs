using CX.Engine.Common;
using CX.Engine.Common.Meta;
using CX.Engine.Common.Python;
using CX.Engine.Common.Stores.Binary;
using CX.Engine.Common.Tracing;
using Microsoft.Extensions.Options;

namespace CX.Engine.DocExtractors.Text;

public class PythonDocX : IDocumentTextExtractor
{
    private readonly IBinaryStore _store;
    private readonly PythonDocXOptions _options;
    private readonly PythonProcess _python;

    public PythonDocX(IOptions<PythonDocXOptions> options, IServiceProvider sp)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _options.Validate();
        _store = sp.GetRequiredNamedService<IBinaryStore>(_options.BinaryStore);
        _python = sp.GetRequiredNamedService<PythonProcess>(_options.PythonProcess);
    }

    /// <summary>
    /// Extracts the text from a PDF stream.
    /// </summary>
    /// <param name="stream">The DocX stream.  Will be read multiple times.</param>
    /// <param name="meta">The document's metadata.</param>
    public async Task<string> ExtractToTextAsync(Stream stream, DocumentMeta meta)
    {
        return await CXTrace.Current.SpanFor(CXTrace.Section_PythonDocX, null)
            .ExecuteAsync(async span =>
            {
                var cacheKey = await stream.GetSHA256Async();

                var cached = await _store.GetUtf8Async(cacheKey);

                if (cached != null)
                {
                    span.Output = new { Content = cached.Preview(2 * 1024 * 1024), FromCache = true, CacheKey = cacheKey };
                    return cached;
                }

                var content = await _python.StreamToStringViaFilesAsync(_options.ScriptPath, stream);
                span.Output = new { Content = content.Preview(2 * 1024 * 1024), FromCache = false, CacheKey = cacheKey };
                await _store.SetUtf8Async(cacheKey, content);
                
                return content;
            });
    }
}
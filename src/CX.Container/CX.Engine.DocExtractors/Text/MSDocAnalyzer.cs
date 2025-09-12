using System.Text;
using Azure;
using Azure.AI.FormRecognizer.DocumentAnalysis;
using CX.Engine.Common;
using CX.Engine.Common.Meta;
using CX.Engine.Common.Stores.Binary;
using CX.Engine.Common.Tracing;
using Microsoft.Extensions.Options;

namespace CX.Engine.DocExtractors.Text;

public class MSDocAnalyzer : IDocumentTextExtractor
{
    private readonly IBinaryStore _store;

    // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
    private readonly MSDocAnalyzerOptions _options;
    private readonly DocumentAnalysisClient _client;

    public MSDocAnalyzer(IOptions<MSDocAnalyzerOptions> options, IServiceProvider sp)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _options.Validate();
        _client = new(new(_options.Endpoint), new AzureKeyCredential(_options.APIKey));
        _store = sp.GetRequiredNamedService<IBinaryStore>(_options.BinaryStore);
    }

    /// <summary>
    /// Extracts the text from a PDF stream.
    /// </summary>
    /// <param name="stream">The PDF stream.  Will be read multiple times.</param>
    /// <param name="meta">The document meta data.</param>
    public async Task<string> ExtractToTextAsync(Stream stream, DocumentMeta meta)
    {
        return await CXTrace.Current.SpanFor(CXTrace.Section_MSDocAnalyzer, null)
            .ExecuteAsync(async span =>
            {
                var cacheKey = await stream.GetSHA256Async();

                var cached = await _store.GetUtf8Async(cacheKey);

                if (cached != null)
                {
                    span.Output = new { Content = cached.Preview(2 * 1024 * 1024), FromCache = true, CacheKey = cacheKey };
                    return cached;
                }

                return await CXTrace.Current.SpanFor(CXTrace.Section_CallAPI, null)
                    .ExecuteAsync(async _ =>
                    {
                        stream.Position = 0;
                        var res = await _client.AnalyzeDocumentAsync(WaitUntil.Completed, "prebuilt-layout", stream);

                        var sb = new StringBuilder();
                        var pageNo = 0;
                        foreach (var page in res.Value.Pages)
                        {
                            pageNo++;
                            sb.AppendLine($"--- PAGE {pageNo} ---");
                            foreach (var line in page.Lines)
                                sb.AppendLine(line.Content);
                            sb.AppendLine();
                        }

                        var content = sb.ToString();
                        span.Output = new { Content = content.Preview(2 * 1024 * 1024), FromCache = false, CacheKey = cacheKey };

                        await _store.SetUtf8Async(cacheKey, content);

                        return content;
                    });
            });
    }
}
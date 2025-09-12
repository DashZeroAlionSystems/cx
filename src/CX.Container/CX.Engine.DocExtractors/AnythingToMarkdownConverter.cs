using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using CX.Engine.Common;
using CX.Engine.Common.Json;
using CX.Engine.Common.Meta;
using CX.Engine.Common.Python;
using CX.Engine.Common.Stores.Binary;
using CX.Engine.Common.Tracing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aela.Server.Converters
{
    public class AnythingToMarkdownExtractor
    {
        private readonly ILogger<AnythingToMarkdownExtractor> _logger;
        private readonly AnythingToMarkdownOptions _options;
        private readonly IBinaryStore _store;
        private readonly PythonProcess _python;
        private readonly KeyedSemaphoreSlim _keyedLock = new();

        public AnythingToMarkdownExtractor(
            IOptions<AnythingToMarkdownOptions> options, 
            IServiceProvider sp, 
            ILogger<AnythingToMarkdownExtractor> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _options.Validate();
            _store = sp.GetRequiredNamedService<IBinaryStore>(_options.BinaryStore);
            _python = sp.GetRequiredNamedService<PythonProcess>(_options.PythonProcess);
        }

        public async Task<string> ExtractToTextAsync(Stream stream, DocumentMeta meta)
        {
            return await ConvertToMarkdownAsync(stream, meta);
        }

        public async Task<string> ConvertToMarkdownAsync(Stream inputStream, DocumentMeta meta)
        {
            return await CXTrace.Current.SpanFor(CXTrace.Section_AnythingToMarkdown, null)
                .ExecuteAsync(async span =>
                {
                    var cacheKey = await inputStream.GetSHA256Async();
                    using var _ = await _keyedLock.UseAsync(cacheKey);

                    var bytes = await _store.GetBytesAsync(cacheKey);
                    var cached = new MarkdownCacheEntry(bytes);

                    if (bytes != null)
                    {
                        span.Output = new
                        {
                            Content = cached.Content?.Preview(2 * 1024 * 1024),
                            ExtractionErrors = cached.ExtractionErrors,
                            FromCache = true,
                            CacheKey = cacheKey
                        };

                        if (cached.ExtractionErrors != null)
                        {
                            foreach (var err in cached.ExtractionErrors)
                                _logger.LogWarning("AnythingToMarkdownExtractor: {Errors}", err);

                            (meta.ExtractionErrors ??= []).AddRange(cached.ExtractionErrors);
                        }

                        return cached.Content ?? "";
                    }

                    var extractionErrors = new List<string>();
                    var content = await _python.StreamToStringViaFilesAsync(_options.ScriptPath, inputStream, stdout =>
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
                                _logger.LogWarning("AnythingToMarkdownExtractor: {Errors}", errors);
                                extractionErrors.Add(page.HasValue
                                    ? $"Page {page.Value}: {errors}"
                                    : errors);
                            }
                        });

                        if (extractionErrors.Count > 0)
                            (meta.ExtractionErrors ??= []).AddRange(extractionErrors);
                    });

                    var entry = new MarkdownCacheEntry
                    {
                        Content = content
                    };
                    entry.ExtractionErrors.AddRange(extractionErrors);

                    span.Output = new
                    {
                        Content = content.Preview(2 * 1024 * 1024),
                        FromCache = false,
                        CacheKey = cacheKey
                    };
                    
                    await _store.SetBytesAsync(cacheKey, entry.GetBytes());
                    return content;
                });
        }
    }

    public class MarkdownCacheEntry
    {
        public const int Magic = 0x2807F3FB;

        public string Content;
        public readonly List<string> ExtractionErrors = new();

        public MarkdownCacheEntry() { }

        public byte[] GetBytes()
        {
            var ms = new MemoryStream();
            var bw = new BinaryWriter(ms);
            Serialize(bw);
            bw.Flush();
            return ms.ToArray();
        }

        public void Serialize(BinaryWriter bw)
        {
            bw.Write(Magic);
            bw.Write(1); //Version
            bw.WriteNullable(Content);

            bw.Write7BitEncodedInt(ExtractionErrors.Count);
            foreach (var err in ExtractionErrors)
                bw.Write(err);
        }

        public MarkdownCacheEntry(byte[] bytes)
        {
            if (bytes != null)
                Populate(bytes);
        }

        private void Populate(byte[] bytes)
        {
            if (bytes.Length < 9)
            {
                Content = Encoding.UTF8.GetString(bytes);
                return;
            }

            using var ms = new MemoryStream(bytes);
            using var br = new BinaryReader(ms);

            if (br.ReadInt32() != Magic)
            {
                Content = Encoding.UTF8.GetString(bytes);
                return;
            }

            if (br.ReadInt32() != 1)
            {
                Content = Encoding.UTF8.GetString(bytes);
                return;
            }

            Content = br.ReadStringNullable();

            var count = br.Read7BitEncodedInt();
            for (var i = 0; i < count; i++)
                ExtractionErrors.Add(br.ReadString());
        }
    }
}
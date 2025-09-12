using System.Text.Json;
using CX.Engine.Common.Embeddings;
using CX.Engine.Common.Json;
using CX.Engine.Common.Stores.Json;
using CX.Engine.Common.Tracing;
using CX.Engine.TextProcessors.Splitters;
using Flurl.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pinecone;
using Pinecone.Rest;

namespace CX.Engine.Archives.Pinecone;

public abstract class PineconeBaseChunkArchive : BaseChunkArchive, IDisposable
{
    public readonly WaitForSnapshotTask WaitForSnapshot = new();
    public readonly string ArchiveName;

    protected readonly EmbeddingCache EmbeddingCache;
    private readonly ILogger _logger;

    private readonly DynamicSlimLock _slimLock = new(1);
    private readonly IServiceProvider _sp;

    private readonly IDisposable _optionsChangeDisposable;
    private readonly SemaphoreSlim _slimOptionsChange = new(1, 1);

    //Used by DeleteNamespace in AIServer.
    public StateSnapshot Snapshot;

    public class StateSnapshot : IDisposable
    {
        public PineconeOptions Options;
        public PineconeClient Client;
        public IJsonStore VectorTracker;
        public Index<RestTransport> Index = null!;

        public void Dispose()
        {
            Client?.Dispose();
            Index?.Dispose();
        }
    }

    protected PineconeBaseChunkArchive(string name, IOptionsMonitor<PineconeOptions> options, EmbeddingCache embeddingCache, IServiceProvider sp, ILogger logger)
    {
        ArchiveName = name ?? throw new ArgumentNullException(nameof(name));
        EmbeddingCache = embeddingCache;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _sp = sp ?? throw new ArgumentNullException(nameof(sp));

        _optionsChangeDisposable = options.Snapshot(() => Snapshot?.Options, SetOptions, logger, sp);
    }

    private void SetOptions(PineconeOptions newOptions) =>
        WaitForSnapshot.DoAsync(_logger, async () =>
        {
            var snapshot = new StateSnapshot()
            {
                Options = newOptions
            };

            using var _ = await _slimOptionsChange.UseAsync();

            var opts = snapshot.Options;

            if (opts.UseJsonVectorTracker!.Value)
                snapshot.VectorTracker = _sp.GetRequiredNamedService<IJsonStore>(opts.JsonVectorTrackerName!);

            snapshot.Client = new(opts.APIKey);

            try
            {
                snapshot.Index = await snapshot.Client.GetIndex<RestTransport>(opts.IndexName);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Error connecting to Pinecone index {opts.IndexName} with API key starting with {opts.APIKey.Left(8)}: {ex.Message}", ex);
            }

            _slimLock.SetMaxCount(snapshot.Options.MaxConcurrency);

            Snapshot = snapshot;
        });


    public static void LoadCitations(string attachmentsBaseUrl, string citations, TextChunk chunk, ILogger logger)
    {
        var jr = new Utf8JsonReader(Encoding.UTF8.GetBytes(citations));
        jr.ReadArrayOfObject(true,
            (ref Utf8JsonReader jr) =>
            {
                var att = new AttachmentInfo();
                att.Context = chunk.GetAttachmentContextString();

                chunk.Metadata.GetAttachments(true)!.Add(att);
                // ReSharper disable once VariableHidesOuterVariable
                jr.ReadObjectProperties(att,
                    false,
                    (ref Utf8JsonReader jr, AttachmentInfo att, string name) =>
                    {
                        switch (name)
                        {
                            case "name":
                                att.FileName = jr.ReadStringValue();
                                break;
                            case "type":
                                _ = jr.ReadStringValue();
                                break;
                            case "url":
                                att.FileUrl = jr.ReadStringValue();
                                break;
                            case "description":
                                att.Description = jr.ReadStringValue();
                                break;
                            default:
                                jr.SkipPropertyValue();
                                break;
                        }
                    });

                if (!string.IsNullOrWhiteSpace(attachmentsBaseUrl) && !string.IsNullOrWhiteSpace(att.FileUrl))
                {
                    att.DoGetContentStreamAsync = async () =>
                    {
                        var url = (attachmentsBaseUrl + att.FileUrl);
                        try
                        {
                            var httpStream = await url.GetStreamAsync();
                            var memoryStream = await httpStream.CopyToMemoryStreamAsync();
                            return memoryStream;
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, $"Error downloading attachment with url {url}");
                            return new MemoryStream(Encoding.UTF8.GetBytes(ex.GetType().Name + ": " + ex.Message));
                        }
                    };
                }
                else
                    att.DoGetContentStreamAsync =
                        () =>
                            Task.FromResult<Stream>(
                                new MemoryStream("Citation content retrieval is not configured for this Pinecone archive instance."u8.ToArray()));
            });
    }

    public override async Task<List<ArchiveMatch>> RetrieveAsync(ChunkArchiveRetrievalRequest req)
    {
        await WaitForSnapshot;

        var ss = Snapshot;
        var opts = ss.Options;

        var ns = req.GetNamespaceFilter() ?? opts.Namespace;

        var searchEmbeds = await EmbeddingCache.GetAsync(opts.EmbeddingModel, req.QueryString);

        var res = new List<ArchiveMatch>();

        return await CXTrace.Current.SpanFor(CXTrace.Section_RetrieveMatches,
            new
            {
                MinSimilarity = req.MinSimilarity,
                CutoffTokens = req.CutoffTokens,
                MaxChunks = req.MaxChunks,
                ArchiveName = ArchiveName,
                Namespace = ns
            }).ExecuteAsync(async span =>
        {
            List<ScoredVector> chunks;

            await _slimLock.WaitAsync();
            try
            {
                chunks = (await ss.Index.Query(searchEmbeds,
                        (uint)opts.MaxChunksPerPineconeQuery,
                        indexNamespace: ns,
                        includeMetadata: true,
                        includeValues: false))
                    .Where(c => c.Score >= req.MinSimilarity)
                    .ToList();
            }
            finally
            {
                _slimLock.Release();
            }

            foreach (var match in chunks)
            {
                var content = match.Metadata?.GetValueOrDefault("text").Inner?.ToString();

                if (content == null)
                    content = "<No content for this vector in Pinecone>";

                var source = match.Metadata?.GetValueOrDefault("source").Inner?.ToString();

                if (source == null)
                    source = Guid.Empty.ToString();

                var citationInfo = match.Metadata?.GetValueOrDefault("citation_info").Inner?.ToString();

                var chunk = new TextChunk(content, new()
                {
                    SourceDocument = match.Metadata?.GetValueOrDefault("source_document").Inner?.ToString(),
                    SourceDocumentGroup = match.Metadata?.GetValueOrDefault("source_document_group").Inner?.ToString(),
                });

                if (int.TryParse(match.Metadata?.GetValueOrDefault("seq_no").Inner?.ToString(), out var seqNo))
                    chunk.SeqNo = seqNo;

                if (citationInfo != null)
                    LoadCitations(ss.Options.AttachmentsBaseUrl, citationInfo, chunk, _logger);

                if (Guid.TryParse(source, out var documentId))
                    chunk.Metadata.DocumentId = documentId;

                res.Add(new(chunk, match.Score));
            }

            res = OrderAndApplyTokenCutoffAndMaxChunks(res, req.CutoffTokens, req.MaxChunks);
            span.Output = res;
            return res;
        });
    }

    public void Dispose()
    {
        Snapshot?.Dispose();
        _optionsChangeDisposable?.Dispose();
    }
}
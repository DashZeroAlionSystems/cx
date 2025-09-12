using System.Data.Common;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using CX.Engine.Common.Db;
using CX.Engine.Common.Embeddings;
using CX.Engine.Common.PostgreSQL;
using CX.Engine.Common.Tracing;
using CX.Engine.TextProcessors.Splitters;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CX.Engine.Archives.PgVector;

public class PgVectorChunkArchive : BaseChunkArchive, IDisposable
{
    private readonly IDisposable _optionsMonitorDisposable;
    private readonly IServiceProvider _sp;
    private readonly EmbeddingCache _embeddingCache;
    private readonly ILogger _logger;
    private Snapshot _snapshot;

    private class Snapshot
    {
        public PgVectorArchiveOptions Options;
        public PostgreSQLClient Sql;
        public Task InitTask;

        public async Task InitAsync()
        {
            if (Options.AutoEnablePgVectorExtension)
                await Sql.EnsurePgVectorEnabledAsync();

            if (Options.AutoCreateTable)
                await Sql.ExecuteAsync(
                    $"""
                     CREATE TABLE IF NOT EXISTS {new InjectRaw(Options.TableName)} (
                         key varchar({new InjectRaw(Options.KeyLength.ToString(CultureInfo.InvariantCulture))}) PRIMARY KEY,
                         metadata jsonb,
                         embedding VECTOR({new InjectRaw(Options.EmbeddingLength.ToString(CultureInfo.InvariantCulture))})
                     )
                     """);
            
            if (Options.AutoCreateIVFFlatCosineIndex)
                await Sql.ExecuteAsync($"SET maintenance_work_mem = '128MB';  CREATE INDEX ON {new InjectRaw(Options.TableName)} USING ivfflat (embedding vector_cosine_ops) WITH (lists = 100)");
        }
    }

    public class ScoredRow
    {
        public string Key;
        public JsonObject Metadata;
        public float Score;

        public static ScoredRow Map(DbDataReader reader)
        {
            var row = new ScoredRow();
            row.Key = reader.Get<string>("key");
            row.Metadata = (JsonObject)JsonNode.Parse(reader.Get<string>("metadata"));
            row.Score = reader.Get<float>("score");
            return row;
        }
    }

    private void SetSnapshot(PgVectorArchiveOptions options)
    {
        var ss = new Snapshot()
        {
            Options = options
        };

        ss.Sql = _sp.GetRequiredNamedService<PostgreSQLClient>(options.PgClientName);
        ss.InitTask = ss.InitAsync();
        _snapshot = ss;
    }

    public PgVectorChunkArchive(IOptionsMonitor<PgVectorArchiveOptions> monitor, ILogger logger, IServiceProvider sp, [NotNull] EmbeddingCache embeddingCache)
    {
        _sp = sp ?? throw new ArgumentNullException(nameof(sp));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _embeddingCache = embeddingCache ?? throw new ArgumentNullException(nameof(embeddingCache));
        _optionsMonitorDisposable = monitor.Snapshot(() => _snapshot?.Options, SetSnapshot, logger, sp);
    }

    public override Task ImportAsync(TextChunk chunk) => RegisterAsync(chunk, _snapshot);

    private async Task RegisterAsync(TextChunk chunk, Snapshot ss)
    {
        await ss.InitTask;

        if (chunk.Metadata.DocumentId == null)
            throw new ArgumentException($"Chunks need a DocumentId to be persisted using {nameof(PgVectorChunkArchive)}");

        var opts = ss.Options;

        await CXTrace.Current.SpanFor("pg-vector-import-chunk", new
        {
            PgClientName = ss.Options.PgClientName,
            TableName = ss.Options.TableName
        }).ExecuteAsync(async span =>
        {
            var embeddings = await _embeddingCache.GetAsync(opts.EmbeddingModel, chunk.GetSurroundingContextString());
            embeddings = embeddings.RoundAndReplace(5);
            var id = chunk.CalculatePineconeId();
            var metadata = new Dictionary<string, object>()
            {
                ["text"] = chunk.GetContextString(),
                ["source"] = chunk.Metadata.DocumentId.ToString(),
                ["source_document"] = chunk.Metadata.SourceDocument,
                ["seq_no"] = chunk.SeqNo,
                ["info"] = chunk.Metadata.Info
            };

            if (chunk.Metadata.SourceDocumentGroup != null)
                metadata["source_document_group"] = chunk.Metadata.SourceDocumentGroup;

            var citationInfo = PineconeChunkArchive.EncodeAttachmentsToCitationInfo(chunk);
            if (citationInfo != null)
                metadata["citation_info"] = citationInfo;

            var metadata_json = JsonSerializer.Serialize(metadata);

            await ss.Sql.ExecuteAsync($"""
                INSERT INTO {new InjectRaw(opts.TableName)} (key, metadata, embedding)
                VALUES ({id}, {metadata_json}::jsonb, {embeddings}::vector)
                ON CONFLICT (key) DO UPDATE SET metadata = {metadata_json}::jsonb, embedding = {embeddings}::vector
            """);
            span.Output = new { ChunkId = id, SourceDocument = chunk.Metadata.SourceDocument, SeqNo = chunk.SeqNo, CitationInfo = citationInfo };
        });
    }

    public override async Task ClearAsync()
    {
        var ss = _snapshot;

        await ss.InitTask;
        
        await ss.Sql.ExecuteAsync($"SET maintenance_work_mem = '128MB'; TRUNCATE TABLE {new InjectRaw(ss.Options.TableName)}");
    }
    
    public override async Task<List<ArchiveMatch>> RetrieveAsync(ChunkArchiveRetrievalRequest req)
    {
        var ss = _snapshot;

        await ss.InitTask;

        var searchEmbeds = (await _embeddingCache.GetAsync(ss.Options.EmbeddingModel, req.QueryString)).RoundAndReplace(5);

        return await CXTrace.Current.SpanFor(CXTrace.Section_RetrieveMatches,
            new
            {
                MinSimilarity = req.MinSimilarity,
                CutoffTokens = req.CutoffTokens,
                MaxChunks = req.MaxChunks,
                PgClientName = ss.Options.PgClientName,
                TableName = ss.Options.TableName
            }).ExecuteAsync(async span =>
        {
            var where = req.Components.GetValueOrDefault<PgVectorAppendWhere>();
            
            var cmd = NpgsqlCommandInterpolatedStringHandler.GetCommand($"""
                                                                         SELECT key, metadata, 1 - (embedding <=> {searchEmbeds}::vector) AS score 
                                                                         FROM {new InjectRaw(ss.Options.TableName)}
                                                                         WHERE 1 - (embedding <=> {searchEmbeds}::vector) >= {req.MinSimilarity}
                                                                         {new InjectRaw(where?.Where)}
                                                                         ORDER BY 1 - (embedding <=> {searchEmbeds}::vector) DESC
                                                                         LIMIT {new InjectRaw(ss.Options.MaxChunksPerQuery.ToString(CultureInfo.InvariantCulture))}
                                                                         """);
            
            if (where?.Parameters != null)
                foreach (var par in where.Parameters)
                    cmd.Parameters.AddWithValue(par.Key, par.Value ?? DBNull.Value);
            
            var matches = await ss.Sql.ListAsync(cmd, ScoredRow.Map);

            var res = new List<ArchiveMatch>();

            foreach (var match in matches)
            {
                var content = match.Metadata?["text"]?.GetValue<string>();

                if (content == null)
                    content = "<No content for this vector in Postgres>";

                var source = match.Metadata?["source"]?.GetValue<string>();

                if (source == null)
                    source = Guid.Empty.ToString();

                var citationInfo = match.Metadata?["citation_info"]?.GetValue<string>();

                var chunk = new TextChunk(content, new()
                {
                    SourceDocument = match.Metadata?["source_document"]?.GetValue<string>(),
                    SourceDocumentGroup = match.Metadata?["source_document_group"]?.GetValue<string>(),
                });

                var seqNo = match.Metadata?["seq_no"]?.GetValue<int>();
                if (seqNo.HasValue)
                    chunk.SeqNo = seqNo.Value;

                chunk.Metadata.Info = match.Metadata?["info"].ToJsonDocument();

                if (citationInfo != null)
                    PineconeBaseChunkArchive.LoadCitations(ss.Options.AttachmentsBaseUrl, citationInfo, chunk, _logger);

                if (Guid.TryParse(source, out var documentId))
                    chunk.Metadata.DocumentId = documentId;

                res.Add(new(chunk, match.Score));
            }

            res = OrderAndApplyTokenCutoffAndMaxChunks(res, req.CutoffTokens, req.MaxChunks);
            span.Output = res;
            return res;
        });
    }

    public override async Task RemoveDocumentAsync(Guid documentId)
    {
        var ss = _snapshot;

        await ss.InitTask;
        
        await ss.Sql.ExecuteAsync($"DELETE FROM {new InjectRaw(ss.Options.TableName)} WHERE metadata->>'source' = {documentId.ToString()}");
    }

    public override async Task ImportAsync(Guid documentId, List<TextChunk> chunks)
    {
        var ss = _snapshot;

        var seqNo = 1;
        foreach (var chunk in chunks)
        {
            chunk.Metadata.DocumentId = documentId;
            chunk.SeqNo = seqNo++;
        }

        await Task.WhenAll(chunks.Select(c => RegisterAsync(c, ss)));
    }

    public void Dispose()
    {
        _optionsMonitorDisposable?.Dispose();
    }
}
using System.Text.Json;
using System.Text.Json.Nodes;
using CX.Engine.Common.Embeddings;
using CX.Engine.Common.Tracing;
using CX.Engine.TextProcessors.Splitters;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pinecone;

namespace CX.Engine.Archives.Pinecone;

public sealed class PineconeChunkArchive : PineconeBaseChunkArchive
{
    public PineconeChunkArchive(string name, IOptionsMonitor<PineconeOptions> options, EmbeddingCache embeddingCache, IServiceProvider sp, ILogger logger) : base(name,
        options,
        embeddingCache,
        sp,
        logger)
    {
    }

    public static string EncodeAttachmentsToCitationInfo(TextChunk chunk)
    {
        var atts = chunk.Metadata.GetAttachments(false);

        if (atts == null)
            return null;

        var any = false;
        using var ms = new MemoryStream();
        using var jw = new Utf8JsonWriter(ms);

        jw.WriteStartArray();

        foreach (var att in atts)
        {
            if (att == null)
                continue;

            any = true;

            jw.WriteStartObject();
            jw.WriteString("name", att.FileName);
            jw.WriteString("type", "form");
            jw.WriteString("url", att.FileUrl);
            jw.WriteString("description", att.Description);
            jw.WriteEndObject();
        }

        if (!any)
            return null;

        jw.WriteEndArray();
        jw.Flush();
        return Encoding.UTF8.GetString(ms.ToMemory().Span);
    }

    public override Task ImportAsync(TextChunk chunk) => RegisterAsync(chunk, null);

    public async Task RegisterAsync(TextChunk chunk, string ns)
    {
        if (chunk.Metadata.DocumentId == null)
            throw new ArgumentException($"Chunks need a DocumentId to be persisted using {nameof(PineconeChunkArchive)}");

        await WaitForSnapshot;

        var ss = Snapshot;
        var opts = ss.Options;

        if (ns == null)
            ns = opts.Namespace;

        await CXTrace.Current.SpanFor("pinecone-import-chunk", new
        {
            Archive = ArchiveName,
            Namespace = ns
        }).ExecuteAsync(async span =>
        {
            var embeddings = await EmbeddingCache.GetAsync(opts.EmbeddingModel, chunk.GetSurroundingContextString());
            var v = new Vector
            {
                Id = chunk.CalculatePineconeId(),
                Values = embeddings,
                Metadata = new()
                {
                    ["text"] = chunk.GetContextString(),
                    ["source"] = chunk.Metadata.DocumentId.ToString(),
                    ["seq_no"] = chunk.SeqNo,
                }
            };
            
            if (chunk.Metadata.SourceDocument != null)
                v.Metadata["source_document"] = chunk.Metadata.SourceDocument;
            
            if (chunk.Metadata.Info != null)
                throw new NotSupportedException($"{nameof(chunk.Metadata.Info)} is not supported for {nameof(PineconeChunkArchive)} yet");

            if (chunk.Metadata.SourceDocumentGroup != null)
                v.Metadata["source_document_group"] = chunk.Metadata.SourceDocumentGroup;

            var citationInfo = EncodeAttachmentsToCitationInfo(chunk);
            if (citationInfo != null)
                v.Metadata["citation_info"] = citationInfo;

            await ss.Index.Upsert([v], ns);
            span.Output = new { ChunkId = v.Id, SourceDocument = chunk.Metadata.SourceDocument, SeqNo = chunk.SeqNo, CitationInfo = citationInfo };
        });
    }

    public override Task ClearAsync() => ClearAsync(null);

    public async Task ClearAsync(string ns)
    {
        await WaitForSnapshot;
        try
        {
            var ss = Snapshot;
            var opts = ss.Options;

            if (string.IsNullOrWhiteSpace(ns))
                ns = opts.Namespace;

            await ss.Index.DeleteAll(ns);
        }
        catch (HttpRequestException ex)
        {
            if (ex.Message.Contains("Namespace not found"))
                return;

            throw;
        }
    }

    public override async Task RemoveDocumentAsync(Guid documentId)
    {
        await WaitForSnapshot;
        var ss = Snapshot;
        var opts = ss.Options;

        if (opts.UseJsonVectorTracker!.Value)
        {
            VectorTrackerObject trackerData;
            var node = await ss.VectorTracker.GetAsync<JsonNode>(documentId.ToString());
            if (node != null)
            {
                if (node.GetValueKind() == JsonValueKind.Object)
                    trackerData = node.Deserialize<VectorTrackerObject>();
                else if (node.GetValueKind() == JsonValueKind.Number)
                    try
                    {
                        trackerData = new()
                        {
                            Count = node.Deserialize<int>(),
                            Namespace = opts.Namespace
                        };
                    }
                    catch
                    {
                        throw new InvalidOperationException($"Invalid data in VectorTracker: expected int or {nameof(VectorTrackerObject)}");
                    }
                else
                    throw new InvalidOperationException($"Invalid data in VectorTracker: expected int or {nameof(VectorTrackerObject)}");
            }
            else
                return;

            var ids = new List<string>();
            for (var i = 1; i <= trackerData.Count; i++)
                ids.Add($"{documentId}.{i}");
            if (ids.Count > 0)
                try
                {
                    await ss.Index.Delete(ids, trackerData.Namespace ?? opts.Namespace);
                }
                catch (Exception ex) when (ex.Message.Contains("Namespace not found"))
                {
                    // Ignore
                }

            await ss.VectorTracker.DeleteAsync(documentId.ToString());
        }
        else
        {
            try
            {
                await ss.Index.Delete(new MetadataMap { ["source"] = documentId.ToString() }, opts.Namespace);
            }
            catch (Exception ex) when (ex.Message.Contains("Namespace not found"))
            {
                // Ignore
            }
        }
    }

    public override Task ImportAsync(Guid documentId, List<TextChunk> chunks) => RegisterAsync(documentId, chunks, null);

    public class VectorTrackerObject
    {
        public int Count { get; set; }
        public string Namespace { get; set; }
    }

    public async Task RegisterAsync(Guid documentId, List<TextChunk> chunks, string ns)
    {
        var ss = Snapshot;
        ns ??= ss.Options.Namespace;

        if (ss.Options.UseJsonVectorTracker!.Value)
            await ss.VectorTracker.SetAsync(documentId.ToString(), new VectorTrackerObject
            {
                Count = chunks.Count,
                Namespace = ns
            });

        var seqNo = 1;
        foreach (var chunk in chunks)
        {
            if (chunk.Metadata.DocumentId != documentId)
                throw new InvalidOperationException("All chunks must have the same DocumentId");

            if (chunk.SeqNo != seqNo)
                throw new InvalidOperationException("Chunks must be ordered by SeqNo starting at 1");

            seqNo++;
        }

        await Task.WhenAll(chunks.Select(c => RegisterAsync(c, ns)));
    }
}
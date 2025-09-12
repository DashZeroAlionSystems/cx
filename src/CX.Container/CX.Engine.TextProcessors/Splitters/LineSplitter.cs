using CX.Engine.Common;
using CX.Engine.Common.Tracing;
using Microsoft.Extensions.Options;

namespace CX.Engine.TextProcessors.Splitters;

public class LineSplitter
{
    private readonly LineSplitterOptions _options;
    public List<int> PageNoLimit;

    public LineSplitter(IOptions<LineSplitterOptions> options)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _options.Validate();
    }

    public async Task<List<TextChunk>> ChunkAsync(LineSplitterRequest req)
    {
        req.Validate();
        
        return await CXTrace.Current.SpanFor(CXTrace.Section_Chunk,
                new
                {
                    Document = req.Document.Preview(2 * 1024 * 1024),
                    SegmentTokenLimit = _options.SegmentTokenLimit
                })
            .ExecuteAsync(span =>
            {
                var segmenter = new LineSplitterSegmenter(req);
                segmenter.PageNoLimit = PageNoLimit;
                segmenter.TokenLimit = _options.SegmentTokenLimit;
                segmenter.Parse();

                var chunks = new List<TextChunk>();
                
                var seqNo = 1;
                
                foreach (var seg in segmenter.Segments)
                    if (seg is TextSegment text)
                    {
                        var chunk = text.ToChunk(req);
                        
                        chunk.SeqNo = seqNo;
                        seqNo++;
                        
                        chunks.Add(chunk);
                        CXTrace.Current.Event("gen-chunk",
                            $"Chunk generated",
                            CXTrace.ObservationLevel.DEFAULT,
                            new
                            {
                                SeqNo = seqNo,
                                ContextString = chunk.GetContextString(),
                                SurroundingContextString = chunk.GetSurroundingContextString()
                            });
                    }

                for (var i = 0; i < chunks.Count; i++)
                {
                    if (i > 0)
                        chunks[i].PrevChunk = chunks[i - 1];
                    if (i < chunks.Count - 1)
                        chunks[i].NextChunk = chunks[i + 1];
                }

                span.Output = new
                {
                    ChunkCount = chunks.Count
                };
                return Task.FromResult(chunks);
            });
    }
}
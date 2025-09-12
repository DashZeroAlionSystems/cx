using CX.Engine.Common;

namespace CX.Engine.TextProcessors.Splitters;

public class TextSegment : LineSplitterSegment
{
    private string _content = null!;
    public bool ContainsTableContents;

    public string Content
    {
        get => _content;
        set
        {
            _content = value;
            EstTokens = TokenCounter.CountTokens(value);
        }
    }

    public int EstTokens { get; private set; }
    public int EoLStrength;
    public readonly CXMeta Metadata = new();
    public bool ContentIsTableOrRow => Content.StartsWith('|') && Content.EndsWith('|');

    public TextSegment(string content)
    {
        Content = content;
        EstTokens = TokenCounter.CountTokens(Content);
    }

    public override string ToString() => $"{EstTokens:#,##0} tok | {Content}";

    public TextChunk ToChunk(LineSplitterRequest req)
    {
        var chunkText = Content;
        var pageNos = Metadata.GetPageNos(false);
        var firstPageNo = pageNos?.Min();

        if (!chunkText.StartsWith("---") && firstPageNo.HasValue)
            chunkText = $"--- PAGE {firstPageNo} ---\n{chunkText}";

        var chunk = new TextChunk(chunkText, Metadata);
        chunk.Segment = this;

        if (req.AttachPageImages && pageNos?.Count > 0)
        {
            if (Metadata.DocumentId == null)
                throw new InvalidOperationException("Cannot attach page images when no document id has been set");
            
            var atts = Metadata.GetAttachments(true)!;
            foreach (var page in pageNos)
                atts.Add(new()
                {
                    FileName = (Metadata.SourceDocument + $" Page {page}.jpg").Trim(),
                    FileUrl = "/api/page-images/" + Metadata.DocumentId + "/" + page
                });
        }

        if (ContainsTableContents)
            //NB: this affects the Segment's metadata too, which is irrelevant for now
            chunk.Metadata.ContainsTables = true;

        chunk.Metadata.Info = Metadata.Info;

        return chunk;
    }

    public override bool CanMergeWith(LineSplitterSegment next, int TokenLimit, int stage)
    {
        if (next is not TextSegment ts)
            return false;

        if (stage == 1 && ContentIsTableOrRow != ts.ContentIsTableOrRow)
            return false;

        return EstTokens + ts.EstTokens < TokenLimit;
    }

    public override TextSegment Merge(LineSplitterSegment next)
    {
        var ts = (TextSegment)next;

        var res = new TextSegment(Content + "\r\n" + ts.Content)
        {
            EoLStrength = ts.EoLStrength
        };
        res.Metadata.MergeMeta(ts.Metadata);
        res.Metadata.MergeMeta(Metadata);
        res.ContainsTableContents = ContentIsTableOrRow || ts.ContentIsTableOrRow || ContainsTableContents ||
                                    ts.ContainsTableContents;
        return res;
    }

    public TextSegment InheritsFrom(TextSegment src)
    {
        ContainsTableContents = src.ContainsTableContents;
        EoLStrength = src.EoLStrength;
        Metadata.AssignFrom(src.Metadata);
        return this;
    }
}
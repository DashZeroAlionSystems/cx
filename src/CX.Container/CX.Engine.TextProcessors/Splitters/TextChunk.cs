using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using CX.Engine.Common;
using CX.Engine.Common.Json;

namespace CX.Engine.TextProcessors.Splitters;

public class TextChunk : ISerializeJson
{
    public int SeqNo;
    [JsonInclude] public readonly string Content;
    [JsonInclude] public readonly int EstTokens;
    [JsonInclude] public readonly CXMeta Metadata;

    public TextChunk PrevChunk;
    public TextChunk NextChunk;
    public LineSplitterSegment Segment;

    private string _metadataString;
    private string _contextString;
    private string _surroundingContextString;
    private string _attachmentContextString;

    public string GetMetadataString()
    {
        if (_metadataString != null)
            return _metadataString;

        var sb = new StringBuilder();
        foreach (var s in Metadata)
        {
            //Page numbers are embedded into each chunk rather than added to the top of the chunk.
            //Filenames are internal meta and not shared with end users or agents.
            if (s.Key is CXMeta.Key_PageNos or CXMeta.Key_ContainsTables or CXMeta.Key_Attachments
                or CXMeta.Key_DocumentId or CXMeta.Key_SourceDocumentGroup or CXMeta.Key_Info)
                continue;

            if (s.Value is HashSet<int> hi)
                sb.AppendLine($"*{s.Key}*: {hi.Aggregate("", (acc, i) => acc + i + ", ")}");
            else if (s.Value is HashSet<string> hs)
                sb.AppendLine($"*{s.Key}*: {hs.Aggregate("", (acc, si) => acc + si.DoubleQuoteAndEscape() + ", ")}");
            else if (s.Key is CXMeta.Key_SourceDocument or CXMeta.Key_SourceDocumentGroup)
                sb.AppendLine($"*{s.Key}*: {Path.ChangeExtension(s.Value?.ToString(), "").RemoveTrailing(".")}");
            else
                sb.AppendLine($"*{s.Key}*: {s.Value}");
        }

        sb.AppendLine();
        return _metadataString = sb.ToString();
    }

    public string GetContextString()
    {
        if (_contextString != null)
            return _contextString;

        return _contextString = (GetMetadataString() + Content).Trim();
    }

    public string GetAttachmentContextString()
    {
        if (_attachmentContextString != null)
            return _attachmentContextString;

        var sb = new StringBuilder();
        if (Metadata.SourceDocument != null)
            sb.Append("*Source Document:* " + Metadata.SourceDocument);
        if (Metadata.DocumentDescription != null)
            sb.Append("*Source Document Description:* " + Metadata.DocumentDescription);
        if (Metadata.Tags != null)
            sb.Append("*Tags:* " + Metadata.Tags);

        return _attachmentContextString = sb.ToString().Trim();
    }

    public string GetSurroundingContextString()
    {
        if (_surroundingContextString != null)
            return _surroundingContextString;

        var sb = new StringBuilder();
        sb.Append(GetMetadataString());

        if (PrevChunk != null)
            sb.AppendLine(PrevChunk.Content);

        sb.AppendLine(Content);

        if (NextChunk != null)
            sb.AppendLine(NextChunk.Content);

        return _surroundingContextString = sb.ToString().Trim();
    }


    public TextChunk(string content, CXMeta metadata = null)
    {
        Content = content;
        Metadata = metadata ?? new();
        EstTokens = TokenCounter.CountTokens(GetContextString());
    }

    public void Serialize(Utf8JsonWriter jw)
    {
        jw.WriteStartObject();
        jw.WritePropertyName("content");
        jw.WriteStringValue(Content);
        jw.WritePropertyName("estTokens");
        jw.WriteNumberValue(EstTokens);
        jw.WritePropertyName("metadata");
        Metadata.Serialize(jw);
        jw.WriteEndObject();
    }

    public override string ToString()
    {
        if (Metadata.GetPageNos(false) == null)
            return $"{EstTokens:#,##0} tok | {Metadata.SourceDocument} {Content.Preview(20)}";
        else
            return $"{EstTokens:#,##0} tok | {Metadata.SourceDocument} p. {string.Join(",", Metadata.GetPageNos(false) ?? [])}";
    }

    public static implicit operator TextChunk(string s) => new(s) { Metadata = { DocumentId = s.GetSHA256Guid() } };
}
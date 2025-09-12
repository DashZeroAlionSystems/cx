using CX.Engine.TextProcessors.Splitters;

namespace CX.Engine.Archives;

public record ArchiveMatch([property: JsonInclude] TextChunk Chunk, [property: JsonInclude] double Score)
{
    private string _groupId;
    
    public override string ToString() => $"{Score:#,##0.0%} match: {Chunk}";

    /// <summary>
    /// Prefers <see cref="CXMeta.SourceDocumentGroup"/> with a fallback to <see cref="CXMeta.SourceDocument"/>
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    public string GroupId => _groupId ??= Chunk.Metadata.SourceDocumentGroup ??
                                  Chunk.Metadata.SourceDocument ??
                                  "<No group>";
}
using CX.Engine.Common.Meta;

namespace CX.Engine.TextProcessors.Splitters;

public class LineSplitterRequest
{
    public readonly string Document;
    public readonly DocumentMeta DocumentMeta;
    public bool AttachPageImages;
    
    public LineSplitterRequest(string document, DocumentMeta docMeta = null)
    {
        Document = document ?? throw new ArgumentNullException(nameof(document));
        DocumentMeta = docMeta ?? new();
    }
    
    public void Validate()
    {
        if (AttachPageImages && !DocumentMeta.Id.HasValue)
            throw new InvalidOperationException($"{nameof(DocumentMeta.Id)} must be set when {nameof(AttachPageImages)} is true");
    }
}
using CX.Engine.Common;
using JetBrains.Annotations;

namespace CX.Engine.Importing;

[PublicAPI]
public class VectorLinkImportJob
{
    public string Description;
    public Guid DocumentId;
    public List<AttachmentInfo> Attachments;
    public HashSet<string> Tags;
    public string SourceDocumentDisplayName = null!;

    public bool? ExtractImages;
    public bool? PreferImageTextExtraction;
    public bool? TrainCitations;
    public bool? AttachToSelf;
    public bool? AttachPageImages;
    public MemoryStream DocumentContent = null!;
    public string Archive;

    public void Validate()
    {
        if (DocumentId == Guid.Empty)
            throw new InvalidOperationException($"{nameof(DocumentId)} must be set");
        
        if (SourceDocumentDisplayName == null)
            throw new InvalidOperationException($"{nameof(SourceDocumentDisplayName)} must be set");
        
        if (DocumentContent == null)
            throw new InvalidOperationException($"{nameof(DocumentContent)} must be set");
    }
}
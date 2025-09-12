using CX.Engine.Common;
using JetBrains.Annotations;

namespace CX.Engine.Importing;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class VectorLinkImporterOptions : IValidatable
{
    public string ArchiveName { get; set; } = null!;
    public string ProdRepoName { get; set; }
    public string AttachmentTrackerName { get; set; } = null!;
    public string[] DocumentProcessors { get; set; }
    public bool ExtractImages { get; set; }
    public bool TrainCitations { get; set; }
    public bool PreferImageTextExtraction { get; set; }
    public bool? DefaultAttachPageImages { get; set; }

    public bool AttachToSelf { get; set; }
    public int MaxConcurrency { get; set; } = 1;

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(ArchiveName))
            throw new ArgumentException($"{nameof(VectorLinkImporterOptions)}.{nameof(ArchiveName)} is required");
        
        if (string.IsNullOrWhiteSpace(AttachmentTrackerName))
            throw new ArgumentException($"{nameof(VectorLinkImporterOptions)}.{nameof(AttachmentTrackerName)} is required");

        if (MaxConcurrency < 1)
            throw new ArgumentException($"{nameof(VectorLinkImporterOptions)}.{nameof(MaxConcurrency)} must be at least 1");
    }
}
using System.ComponentModel;
using CX.Engine.Common;
using JetBrains.Annotations;

namespace CX.Engine.Importers;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class DiskImporterOptions
{
    public List<ImportJobMeta> Imports { get; set; } = [];
    public string ContentDirectory { get; set; } = null!;
    public string Archive { get; set; } = null!;

    public bool ClearArchive { get; set; }
    public bool ExtractImages { get; set; }
    public bool WriteExtractTextToFile { get; set; }
    public int LogProgressPerFile { get; set; } = 2;

    public bool PreferVisionTextExtractor { get; set; }
    
    [DefaultValue(1)]
    public int MaxConcurrency { get; set; } = 1;

    public void Validate()
    {
        if (!Directory.Exists(ContentDirectory))
            throw new InvalidOperationException($"Content directory {ContentDirectory} does not exist.");
        
        foreach (var docMeta in Imports)
        {
            if (docMeta == null)
                throw new InvalidOperationException($"Found null document meta in {nameof(DiskImporterOptions)}.{nameof(Imports)}.");

            docMeta.Validate(ContentDirectory);
        }
        
        if (string.IsNullOrWhiteSpace(Archive))
            throw new InvalidOperationException($"{nameof(DiskImporterOptions)}.{nameof(Archive)} is required.");
        
        if (MaxConcurrency < 1)
            throw new InvalidOperationException($"{nameof(DiskImporterOptions)}.{nameof(MaxConcurrency)} must be at least 1.");
    }
}
using JetBrains.Annotations;

namespace CX.Engine.Importing.Prod;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class VectormindProdImporterOptions
{
    public string[] Channels { get; set; }
    
    public string ArchiveName { get; set; } = null!;
    public string ProdRepoName { get; set; } = null!;
    public string ProdS3HelperName { get; set; } = null!;
    public string APIBaseUrl { get; set; } = null!;

    public int? MaxConcurrency { get; set; }
    public int SingleDocumentLogLevel { get; set; }
    public bool OnlyImportDocumentsWithAttachments { get; set; }

    public bool ClearArchive { get; set; }

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(ProdRepoName))
            throw new ArgumentException($"{nameof(VectormindProdImporterOptions)}.{nameof(ProdRepoName)} is required");

        if (string.IsNullOrWhiteSpace(ProdS3HelperName))
            throw new ArgumentException($"{nameof(VectormindProdImporterOptions)}.{nameof(ProdS3HelperName)} is required");

        if (string.IsNullOrWhiteSpace(ArchiveName))
            throw new ArgumentException($"{nameof(VectormindProdImporterOptions)}.{nameof(ArchiveName)} is required");
        
        if (MaxConcurrency < 1)
            throw new InvalidOperationException($"{nameof(VectormindProdImporterOptions)}.{nameof(MaxConcurrency)} must be at least 1.");
        
        if (string.IsNullOrWhiteSpace(APIBaseUrl))
            throw new ArgumentException($"{nameof(VectormindProdImporterOptions)}.{nameof(APIBaseUrl)} is required");
    }
}
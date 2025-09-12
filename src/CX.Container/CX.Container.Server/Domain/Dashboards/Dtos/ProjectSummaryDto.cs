namespace CX.Container.Server.Domain.Dashboards.Dtos;

/// <summary>
/// Data Transfer Object representing a project's summary.
/// </summary>
public sealed record ProjectSummaryDto
{
    public int TotalCategories { get; set; }
    public int TotalAssets { get; set; }
    public int TotalNodes { get; set; }
    public int CategoryCompletenessPercentage { get; set; }
    public int TrainedAssetPercentage { get; set; }
    public int DoneSourceDocuments { get; set; }
    public int ErrorSourceDocuments { get; set; }
    public int PublicBucketSourceDocuments { get; set; }
    public int ProcessingBucketSourceDocuments { get; set; }    
}

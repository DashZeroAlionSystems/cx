namespace CX.Container.Server.Domain.Dashboards.Dtos;

/// <summary>
/// Data Transfer Object representing the client's summary.
/// </summary>
public sealed record ClientSummaryDto
{
    public int TotalDocuments { get; set; }
    public int TotalDoneDocuments { get; set; }
    public int TotalTrainingDoneDocuments { get; set; }
    public int TotalErrorDocuments { get; set; }
    public int TotalDecoratingDocuments { get; set; }
    public int TotalDecoratingDoneDocuments { get; set; }
    public int TotalPrivateBucketDocuments { get; set; }
    public int TotalPublicBucketDocuments { get; set; }
    public int TotalOCRDocuments { get; set; }
    public int TotalQueuedForRetrainDocuments { get; set; }

}

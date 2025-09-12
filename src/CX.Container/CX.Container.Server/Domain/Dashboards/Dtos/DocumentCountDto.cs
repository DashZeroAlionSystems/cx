namespace CX.Container.Server.Domain.Dashboards.Dtos;

/// <summary>
/// Data Transfer Object representing the client's summary.
/// </summary>
public sealed record DocumentCountDto
{
    public int TotalCount { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }
}

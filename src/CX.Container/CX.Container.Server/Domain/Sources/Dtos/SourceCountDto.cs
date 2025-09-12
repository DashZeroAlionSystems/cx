namespace CX.Container.Server.Domain.Sources.Dtos;

/// <summary>
/// Data Transfer Object representing the count of sources for a month of the year.
/// </summary>
public sealed record SourceCountDto
{
    public int TotalCount { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }
}

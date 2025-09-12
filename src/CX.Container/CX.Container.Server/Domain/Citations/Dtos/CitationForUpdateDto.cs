namespace CX.Container.Server.Domain.Citations.Dtos;

/// <summary>
/// Data Transfer Object representing a Citation to be Updated.
/// </summary>
public sealed record CitationForUpdateDto
{
    public string Url { get; set; }

    public string Name { get; set; }

    public string Description { get; set; }
}

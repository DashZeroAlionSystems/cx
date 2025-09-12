namespace CX.Container.Server.Domain.Sources.Dtos;

/// <summary>
/// Data Transfer Object representing a Document Source to be Created.
/// </summary>
public sealed record SourceForCreationDto
{
    /// <summary>
    /// Name of the Source.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Description of the Source.
    /// </summary>
    public string Description { get; set; }
}
namespace CX.Container.Server.Domain.Sources.Dtos;

/// <summary>
/// Data Transfer Object representing a Document Source to be Updated.
/// </summary>
public sealed record SourceForUpdateDto
{
    /// <summary>
    /// New Name of the Source.
    /// </summary>
    public string Name { get; set; }
    
    /// <summary>
    /// New Description of the Source.
    /// </summary>
    public string Description { get; set; }

}

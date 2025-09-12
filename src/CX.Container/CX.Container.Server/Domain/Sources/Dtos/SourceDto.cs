namespace CX.Container.Server.Domain.Sources.Dtos;

/// <summary>
/// Data Transfer Object representing a Document Source.
/// </summary>
public sealed record SourceDto
{
    /// <summary>
    /// Unique Identifier.
    /// </summary>
    public Guid Id { get; set; }
    
    /// <summary>
    /// Name of the Source.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Description of the Source.
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// Number of Source Documents.
    /// </summary>
    public int SourceDocumentCount { get; set; }

}

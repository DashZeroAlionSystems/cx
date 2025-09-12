namespace CX.Container.Server.Domain.SourceDocuments.Dtos;

/// <summary>
/// Data Transfer Object representing a Document.
/// </summary>
public sealed record SourceDocumentForFileUploadDto
{
    /// <summary>
    /// Unique identifier specifying this Document's <see cref="Sources.Source"/>
    /// </summary>
    public Guid? SourceId { get; set; }

    /// <summary>
    /// Unique identifier specifying this Document's <see cref="Nodes.Node"/>
    /// </summary>
    public Guid? NodeId { get; set; }

    /// <summary>
    /// Unique Identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Dispaly Name of the Document.
    /// </summary>
    public string DisplayName { get; set; }

    /// <summary>
    /// Description of the Document.
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// Actual File.
    /// </summary>
    public IFormFile File { get; set; }
}

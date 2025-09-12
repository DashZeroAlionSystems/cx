using System.ComponentModel.DataAnnotations;

namespace CX.Container.Server.Domain.SourceDocuments.Dtos;

/// <summary>
/// Data Transfer Object representing a Document to be Created.
/// </summary>
public sealed record SourceDocumentForCreationDto
{
    /// <summary>
    /// Unique Identifier linking to this Document's <see cref="Sources.Source"/>.
    /// </summary>
    public Guid? SourceId { get; set; }

    /// <summary>
    /// Unique Identifier linking to this Document's <see cref="Nodes.Node"/>.
    /// </summary>
    public Guid? NodeId { get; set; }

    /// <summary>
    /// Name of the Document.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Display Name of the Document.
    /// </summary>
    public string DisplayName { get; set; }

    /// <summary>
    /// Tags associated with the Document.
    /// </summary>
    public string Tags { get; set; }
    
    /// <summary>
    /// Language of the text.
    /// </summary>
    public string Language { get; set; }

    /// <summary>
    /// Description of the Document.
    /// </summary>
    [MaxLength(254)]
    public string Description { get; set; }
    
    /// <summary>
    /// The Source of the Document. Values defined by <see cref="DocumentSourceTypes.DocumentSourceType"/>.
    /// </summary>
    public string DocumentSourceType { get; set; }
    
    /// <summary>
    /// URI of the Document.
    /// </summary>
    public string Url { get; set; }
}

using CX.Container.Server.Domain.Citations.Dtos;
using System.ComponentModel.DataAnnotations;

namespace CX.Container.Server.Domain.SourceDocuments.Dtos;

/// <summary>
/// Data Transfer Object representing a Document to be Updated.
/// </summary>
public sealed record SourceDocumentForUpdateDto
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
    /// Source of the Document. Values defined by <see cref="DocumentSourceTypes.DocumentSourceType"/>.
    /// </summary>
    public string DocumentSourceType { get; set; }
    
    /// <summary>
    /// URI of the Document's location.
    /// </summary>
    public string Url { get; set; }
    
    /// <summary>
    /// Status of the Document. Values defined by <see cref="SourceDocumentStatus.SourceDocumentStatus"/>.
    /// </summary>
    public string Status { get; set; }

    /// <summary>
    /// When the state is error this will contain the error text.
    /// </summary>
    public string ErrorText { get; set; }

    /// <summary>
    /// This will contain the Import Warnings.
    /// </summary>
    public string ImportWarnings { get; set; }


    /// <summary>
    /// Task ID of the Task that extracted text from an Image.
    /// </summary>
    public string OCRTaskID { get; set; }

    /// <summary>
    /// Text that was extracted from an Image.
    /// </summary>
    public string OCRText { get; set; }
    
    /// <summary>
    /// Decorated text.
    /// </summary>
    public string DecoratorText { get; set; }
    
    /// <summary>
    /// Task ID of the Task that decorated the text.
    /// </summary>
    public string DecoratorTaskID { get; set; }
    
    /// <summary>
    /// Task ID of the Task that trained the Document.
    /// </summary>
    public string TrainingTaskID { get; set; }
    
    /// <summary>
    /// Date the Document was trained.
    /// </summary>
    public DateTime DateTrained { get; set; }

    /// <summary>
    /// Citations for file
    /// </summary>
    public List<CitationUploadDto> Citations { get; set; }
}

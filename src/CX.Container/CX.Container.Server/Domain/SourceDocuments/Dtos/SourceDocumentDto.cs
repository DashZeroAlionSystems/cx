using CX.Container.Server.Domain.Citations.Dtos;

namespace CX.Container.Server.Domain.SourceDocuments.Dtos;

/// <summary>
/// Data Transfer Object representing a Document.
/// </summary>
public sealed record SourceDocumentDto
{
    /// <summary>
    /// Unique Identifier.
    /// </summary>
    public Guid Id { get; set; }
    
    /// <summary>
    /// Unique identifier specifying this Document's <see cref="Sources.Source"/>
    /// </summary>
    public Guid? SourceId { get; set; }

    /// <summary>
    /// Unique identifier specifying this Document's <see cref="Nodes.Node"/>
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
    /// Description of the Document.
    /// </summary>
    public string Description { get; set; }
    
    /// <summary>
    /// Tags associated with the Document.
    /// </summary>
    public string Tags { get; set; }
    
    /// <summary>
    /// Language of the text.
    /// </summary>
    public string Language { get; set; }
    
    /// <summary>
    /// The Source of the Document. Values defined by <see cref="DocumentSourceTypes.DocumentSourceType"/>.
    /// </summary>
    public string DocumentSourceType { get; set; }
    
    /// <summary>
    /// URI of the Document.
    /// </summary>
    public string Url { get; set; }
    
    /// <summary>
    /// Indicates whether the Document has been trained.
    /// </summary>
    public bool IsTrained { get; set; }

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
    /// Text that was extracted from an Image.
    /// </summary>
    public string OCRText { get; set; }
    
    /// <summary>
    /// Unique identifier of the task that extracted the OCR Text.
    /// </summary>
    public string OCRTaskID { get; set; }
    
    /// <summary>
    /// The decorated text.
    /// </summary>
    public string DecoratorText { get; set; }
    
    /// <summary>
    /// Unique identifier of the task that decorated the text.
    /// </summary>
    public string DecoratorTaskID { get; set; }
    
    /// <summary>
    /// Unique identifier of the task that trained the Document.
    /// </summary>
    public string TrainTaskID { get; set; }
    
    /// <summary>
    /// Date and Time at which this document was processed for training.
    /// </summary>
    public DateTime DateTrained { get; set; }

    public CitationUrlDto[] Citations { get; set; }
}

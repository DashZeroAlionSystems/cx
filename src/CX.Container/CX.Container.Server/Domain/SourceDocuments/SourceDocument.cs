namespace CX.Container.Server.Domain.SourceDocuments;

using System.ComponentModel.DataAnnotations;
using CX.Container.Server.Domain.Sources;
using CX.Container.Server.Domain.SourceDocuments.Models;
using CX.Container.Server.Domain.SourceDocuments.DomainEvents;
using CX.Container.Server.Domain.DocumentSourceTypes;
using CX.Container.Server.Domain.SourceDocumentStatus;
using System.ComponentModel;
using Microsoft.IdentityModel.Tokens;
using CX.Container.Server.Domain.Nodes;
using System.Diagnostics.CodeAnalysis;
using CX.Container.Server.Domain.Citations;

public class SourceDocument : Entity<Guid>
{
    private readonly List<Citation> _citations = new();
    public IReadOnlyCollection<Citation> Citations => _citations.AsReadOnly();

    public string Name { get; private set; }

    [AllowNull]
    public string? DisplayName { get; private set; }

    [AllowNull]
    public string? Description { get; private set; }

    [AllowNull]
    public string? Tags { get; private set; }

    [AllowNull]
    public string? Language { get; private set; }
    
    [Required]
    public DocumentSourceType DocumentSourceType { get; private set; }

    [AllowNull]
    public string? Url { get; private set; }
    
    [DefaultValue(typeof(SourceDocumentStatus), "New")]
    [Required]
    public SourceDocumentStatus Status { get; private set; }

    [DefaultValue(false)]
    public bool IsTrained { get; private set; } = false;

   
    public string? OCRText { get; private set; }

   
    public string? ImportWarnings { get; private set; }

    
    public string? ErrorText { get; private set; }

    
    public string? DecoratorText { get; private set; }

    
    public string? OCRTaskID { get; private set; }

    
    public string? DecoratorTaskID { get; private set; }

    
    public string? TrainTaskID { get; private set; }

    
    public DateTime? DateTrained { get; private set; }

    public Guid? SourceId { get; private set; }
    public Source Source { get; private set; }

    public Guid? NodeId { get; private set; }
    public Node Node { get; private set; }

    [AllowNull]
    public string? TrainingTaskID { get; private set; }

    // Add Props Marker -- Deleting this comment will cause the add props utility to be incomplete


    public static SourceDocument Create(SourceDocumentForCreation sourceDocumentForCreation)
    {
        var newSourceDocument = new SourceDocument
        {
            Id = Guid.NewGuid(),
            SourceId = sourceDocumentForCreation.SourceId,
            NodeId = sourceDocumentForCreation.NodeId,
            Name = sourceDocumentForCreation.Name,
            DisplayName = sourceDocumentForCreation.DisplayName,
            Description = sourceDocumentForCreation.Description,
            DocumentSourceType = DocumentSourceType.Of(sourceDocumentForCreation.DocumentSourceType) ?? throw new ArgumentNullException(nameof(sourceDocumentForCreation.DocumentSourceType)),
            Url = sourceDocumentForCreation.Url,
            Status = SourceDocumentStatus.PublicBucket(),
            Tags = sourceDocumentForCreation.Tags,
            Language = sourceDocumentForCreation.Language
        };

        newSourceDocument.QueueDomainEvent(new SourceDocumentCreated() { SourceDocument = newSourceDocument });

        return newSourceDocument;
    }

    public SourceDocument Update(SourceDocumentForUpdate sourceDocumentForUpdate)
    {
        if(sourceDocumentForUpdate.SourceId != null)
        {
            SourceId = sourceDocumentForUpdate.SourceId;
        }
        if (sourceDocumentForUpdate.NodeId != null)
        {
            NodeId = sourceDocumentForUpdate.NodeId;
        }
        if (!sourceDocumentForUpdate.Url.IsNullOrEmpty())
        {
            Url = sourceDocumentForUpdate.Url;
        }
        if (!sourceDocumentForUpdate.Status.IsNullOrEmpty())
        {
            Status = SourceDocumentStatus.Of(sourceDocumentForUpdate.Status);
        }
        if (!sourceDocumentForUpdate.Name.IsNullOrEmpty())
        {
            Name = sourceDocumentForUpdate.Name;
        }
        if (!sourceDocumentForUpdate.DisplayName.IsNullOrEmpty())
        {
            DisplayName = sourceDocumentForUpdate.DisplayName;
        }
        if (!sourceDocumentForUpdate.Tags.IsNullOrEmpty())
        {
            Tags = sourceDocumentForUpdate.Tags;
        }
        if (!sourceDocumentForUpdate.Language.IsNullOrEmpty())
        {
            Language = sourceDocumentForUpdate.Language;
        }
        if (!sourceDocumentForUpdate.Description.IsNullOrEmpty())
        {
            Description = sourceDocumentForUpdate.Description;
        }
        if (!sourceDocumentForUpdate.DocumentSourceType.IsNullOrEmpty())
        {
            DocumentSourceType = DocumentSourceType.Of(sourceDocumentForUpdate.DocumentSourceType);
        }
        if (!sourceDocumentForUpdate.OCRTaskID.IsNullOrEmpty())
        {
            OCRTaskID = sourceDocumentForUpdate.OCRTaskID;
        }
        if (!sourceDocumentForUpdate.OCRText.IsNullOrEmpty())
        {
            OCRText = sourceDocumentForUpdate.OCRText;
        }
        if (!sourceDocumentForUpdate.DecoratorText.IsNullOrEmpty())
        {
            DecoratorText = sourceDocumentForUpdate.DecoratorText;
        }
        if (!sourceDocumentForUpdate.DecoratorTaskID.IsNullOrEmpty())
        {
            DecoratorTaskID = sourceDocumentForUpdate.DecoratorTaskID;
        }
        if (!sourceDocumentForUpdate.TrainingTaskID.IsNullOrEmpty())
        {
            TrainingTaskID = sourceDocumentForUpdate.TrainingTaskID;
        }
        if (!sourceDocumentForUpdate.ErrorText.IsNullOrEmpty())
        {
            ErrorText = sourceDocumentForUpdate.ErrorText;
        }

#pragma warning disable CS8073 // The result of the expression is always the same since a value of this type is never equal to 'null'
        if (sourceDocumentForUpdate.DateTrained != null && sourceDocumentForUpdate.DateTrained != default)
        {
            DateTrained = sourceDocumentForUpdate.DateTrained;
            IsTrained = true;
        }
#pragma warning restore CS8073 // The result of the expression is always the same since a value of this type is never equal to 'null'

        QueueDomainEvent(new SourceDocumentUpdated() { Id = Id });
        return this;
    }

    public SourceDocument SetSource(Source source)
    {
        Source = source;
        return this;
    }

    public SourceDocument UpdateState(SourceDocumentStatus status)
    {
        Status = status;
        return this;
    }

    // Add Prop Methods Marker -- Deleting this comment will cause the add props utility to be incomplete

    protected SourceDocument() { } // For EF + Mocking
}

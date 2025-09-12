namespace CX.Container.Server.Domain.SourceDocuments.DomainEvents;

public sealed class SourceDocumentCreated : DomainEvent
{
    public SourceDocument SourceDocument { get; set; }
}

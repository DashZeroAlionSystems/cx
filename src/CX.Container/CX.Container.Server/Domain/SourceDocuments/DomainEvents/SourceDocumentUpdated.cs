namespace CX.Container.Server.Domain.SourceDocuments.DomainEvents;

public sealed class SourceDocumentUpdated : DomainEvent
{
    public Guid Id { get; set; }
}

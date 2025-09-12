namespace CX.Container.Server.Domain.Citations.DomainEvents;

public sealed class CitationUpdated : DomainEvent
{
    public Guid Id { get; set; } 
}
            
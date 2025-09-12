namespace CX.Container.Server.Domain.Citations.DomainEvents;

public sealed class CitationCreated : DomainEvent
{
    public Citation Citation { get; set; } 
}
            
namespace CX.Container.Server.Domain.Sources.DomainEvents;

public sealed class SourceUpdated : DomainEvent
{
    public Guid Id { get; set; } 
}
            
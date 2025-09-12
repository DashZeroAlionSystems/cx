namespace CX.Container.Server.Domain.Nodes.DomainEvents;

public sealed class NodeUpdated : DomainEvent
{
    public Guid Id { get; set; } 
}
            
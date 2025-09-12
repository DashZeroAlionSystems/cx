namespace CX.Container.Server.Domain.Nodes.DomainEvents;

public sealed class NodeCreated : DomainEvent
{
    public Node Node { get; set; } 
}
            
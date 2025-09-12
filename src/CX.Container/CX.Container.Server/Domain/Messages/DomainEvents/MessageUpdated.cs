namespace CX.Container.Server.Domain.Messages.DomainEvents;

public sealed class MessageUpdated : DomainEvent
{
    public Guid Id { get; set; } 
}
            
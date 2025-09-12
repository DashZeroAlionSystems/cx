namespace CX.Container.Server.Domain.Threads.DomainEvents;

public sealed class MessageDeleted : DomainEvent
{
    public Guid Id { get; set; } 
}
            
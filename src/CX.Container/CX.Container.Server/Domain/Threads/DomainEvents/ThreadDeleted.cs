namespace CX.Container.Server.Domain.Threads.DomainEvents;

public sealed class ThreadDeleted : DomainEvent
{
    public Guid Id { get; set; } 
}
            
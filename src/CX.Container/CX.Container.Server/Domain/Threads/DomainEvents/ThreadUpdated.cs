namespace CX.Container.Server.Domain.Threads.DomainEvents;

public sealed class ThreadUpdated : DomainEvent
{
    public Guid Id { get; set; } 
}
            
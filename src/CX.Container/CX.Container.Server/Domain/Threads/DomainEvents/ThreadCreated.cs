namespace CX.Container.Server.Domain.Threads.DomainEvents;

public sealed class ThreadCreated : DomainEvent
{
    public Thread Thread { get; set; } 
}
            
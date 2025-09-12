namespace CX.Container.Server.Domain.Sources.DomainEvents;

public sealed class SourceCreated : DomainEvent
{
    public Source Source { get; set; } 
}
            
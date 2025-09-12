namespace CX.Container.Server.Domain.UiLogs.DomainEvents;

public sealed class UiLogsCreated : DomainEvent
{
    public UiLogs UiLogs { get; set; } 
}
            
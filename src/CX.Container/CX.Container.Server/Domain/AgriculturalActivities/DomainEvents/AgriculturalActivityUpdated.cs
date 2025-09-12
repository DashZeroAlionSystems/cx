namespace CX.Container.Server.Domain.AgriculturalActivities.DomainEvents;

public sealed class AgriculturalActivityUpdated : DomainEvent
{
    public Guid Id { get; set; } 
}
            
namespace CX.Container.Server.Domain.AgriculturalActivities.DomainEvents;

public sealed class AgriculturalActivityCreated : DomainEvent
{
    public AgriculturalActivity AgriculturalActivity { get; set; } 
}
            
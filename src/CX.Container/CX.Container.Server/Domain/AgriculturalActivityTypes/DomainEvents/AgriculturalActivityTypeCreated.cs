namespace CX.Container.Server.Domain.AgriculturalActivityTypes.DomainEvents;

public sealed class AgriculturalActivityTypeCreated : DomainEvent
{
    public AgriculturalActivityType AgriculturalActivityType { get; set; } 
}
            
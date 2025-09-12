namespace CX.Container.Server.Domain.Countries.DomainEvents;

public sealed class CountryCreated : DomainEvent
{
    public Country Country { get; set; } 
}
            
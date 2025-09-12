namespace CX.Container.Server.Domain.Countries.DomainEvents;

public sealed class CountryUpdated : DomainEvent
{
    public Guid Id { get; set; } 
}
            
namespace CX.Container.Server.Domain.Profiles.DomainEvents;

public sealed class ProfileUpdated : DomainEvent
{
    public Guid Id { get; set; } 
}
            
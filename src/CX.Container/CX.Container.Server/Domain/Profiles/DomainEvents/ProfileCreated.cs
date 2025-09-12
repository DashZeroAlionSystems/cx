namespace CX.Container.Server.Domain.Profiles.DomainEvents;

public sealed class ProfileCreated : DomainEvent
{
    public Profile Profile { get; set; } 
}
            
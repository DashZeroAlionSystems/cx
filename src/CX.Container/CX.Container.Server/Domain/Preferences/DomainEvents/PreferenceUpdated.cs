namespace CX.Container.Server.Domain.Preferences.DomainEvents;

public sealed class PreferenceUpdated : DomainEvent
{
    public Guid Id { get; set; } 
}
            
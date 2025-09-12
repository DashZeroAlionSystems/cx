namespace CX.Container.Server.Domain.Preferences.DomainEvents;

public sealed class PreferenceCreated : DomainEvent
{
    public Preference Preference { get; set; } 
}
            
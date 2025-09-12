namespace CX.Container.Server.Domain.Preferences;
using CX.Container.Server.Domain.Preferences.Models;
using CX.Container.Server.Domain.Preferences.DomainEvents;


public class Preference : Entity<Guid>
{
    public string Key { get; private set; }

    public string Value { get; private set; }

    // Add Props Marker -- Deleting this comment will cause the add props utility to be incomplete


    public static Preference Create(PreferenceForCreation preferenceForCreation)
    {
        var newPreference = new Preference();

        newPreference.Key = preferenceForCreation.Key;
        newPreference.Value = preferenceForCreation.Value;

        newPreference.QueueDomainEvent(new PreferenceCreated(){ Preference = newPreference });
        
        return newPreference;
    }

    public Preference Update(PreferenceForUpdate preferenceForUpdate)
    {
        Key = preferenceForUpdate.Key;
        Value = preferenceForUpdate.Value;

        QueueDomainEvent(new PreferenceUpdated(){ Id = Id });
        return this;
    }

    // Add Prop Methods Marker -- Deleting this comment will cause the add props utility to be incomplete
    
    protected Preference() { } // For EF + Mocking
}
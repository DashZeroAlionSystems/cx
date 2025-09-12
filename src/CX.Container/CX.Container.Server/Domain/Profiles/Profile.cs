using CX.Container.Server.Domain.Users;

namespace CX.Container.Server.Domain.Profiles;

using AgriculturalActivities;
using Countries;
using Models;
using DomainEvents;

public class Profile : Entity<Guid>, IHardDelete
{
    public string Name { get; private set; }

    public string AddressLine1 { get; set; }

    public string AddressLine2 { get; set; }

    public string AddressLine3 { get; set; }

    public string City { get; set; }

    public string PostalCode { get; set; }

    public string UserId { get; private set; }
    public User User { get; private set; }

    public Guid? CountryId { get; private set; }
    public Country Country { get; private set; }

    // V2 fields
    public string LocationId { get; set; }
    public string Latitude { get; set; }
    public string Longitude { get; set; }

    private readonly List<AgriculturalActivity> _agriculturalActivities = new();
    public IReadOnlyCollection<AgriculturalActivity> AgriculturalActivities => _agriculturalActivities.AsReadOnly();

    // Add Props Marker -- Deleting this comment will cause the add props utility to be incomplete


    public static Profile Create(ProfileForCreation profileForCreation)
    {
        var newProfile = new Profile();

        newProfile.UserId = profileForCreation.UserId;
        newProfile.Name = profileForCreation.Name;
        newProfile.AddressLine1 = profileForCreation.AddressLine1;
        newProfile.AddressLine2 = profileForCreation.AddressLine2;
        newProfile.AddressLine3 = profileForCreation.AddressLine3;
        newProfile.City = profileForCreation.City;
        newProfile.PostalCode = profileForCreation.PostalCode;
        newProfile.LocationId = profileForCreation.LocationId;
        newProfile.Latitude = profileForCreation.Latitude;
        newProfile.Longitude = profileForCreation.Longitude;

        newProfile.QueueDomainEvent(new ProfileCreated(){ Profile = newProfile });
        
        return newProfile;
    }

    public Profile Update(ProfileForUpdate profileForUpdate)
    {
        Name = profileForUpdate.Name;
        AddressLine1 = profileForUpdate.AddressLine1;
        AddressLine2 = profileForUpdate.AddressLine2;
        AddressLine3 = profileForUpdate.AddressLine3;
        City = profileForUpdate.City;
        PostalCode = profileForUpdate.PostalCode;

        QueueDomainEvent(new ProfileUpdated(){ Id = Id });
        return this;
    }

    public Profile SetCountry(Country country)
    {
        Country = country;
        return this;
    }
    
    public Profile SetUser(User user)
    {
        User = user;
        return this;
    }

    public Profile AddAgriculturalActivity(AgriculturalActivity agriculturalActivity)
    {
        _agriculturalActivities.Add(agriculturalActivity);
        return this;
    }
    
    public Profile RemoveAgriculturalActivity(AgriculturalActivity agriculturalActivity)
    {
        _agriculturalActivities.RemoveAll(x => x.Id == agriculturalActivity.Id);
        return this;
    }

    // Add Prop Methods Marker -- Deleting this comment will cause the add props utility to be incomplete
    
    protected Profile() { } // For EF + Mocking


    // V2 methods
    public static Profile CreateV2(ProfileForCreationV2 profileForCreation)
    {
        var newProfile = new Profile();

        newProfile.UserId = profileForCreation.UserId;
        newProfile.Name = profileForCreation.Name;
        newProfile.LocationId = profileForCreation.LocationId;

        newProfile.QueueDomainEvent(new ProfileCreated() { Profile = newProfile });

        return newProfile;
    }

    public Profile UpdateV2(ProfileForUpdateV2 profileForUpdate)
    {
        Name = profileForUpdate.Name;
        LocationId = profileForUpdate.LocationId;

        QueueDomainEvent(new ProfileUpdated() { Id = Id });
        return this;
    }
}

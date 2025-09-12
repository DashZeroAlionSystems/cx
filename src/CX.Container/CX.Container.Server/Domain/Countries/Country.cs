namespace CX.Container.Server.Domain.Countries;

using CX.Container.Server.Domain.Profiles;
using CX.Container.Server.Domain.Countries.Models;
using CX.Container.Server.Domain.Countries.DomainEvents;


public class Country : Entity<Guid>
{
    public string CountryCode { get; private set; }

    public string Name { get; private set; }

    public IReadOnlyCollection<Profile> Profiles { get; } = new List<Profile>();

    // Add Props Marker -- Deleting this comment will cause the add props utility to be incomplete


    public static Country Create(CountryForCreation countryForCreation)
    {
        var newCountry = new Country();

        newCountry.CountryCode = countryForCreation.CountryCode;
        newCountry.Name = countryForCreation.Name;

        newCountry.QueueDomainEvent(new CountryCreated(){ Country = newCountry });
        
        return newCountry;
    }

    public Country Update(CountryForUpdate countryForUpdate)
    {
        CountryCode = countryForUpdate.CountryCode;
        Name = countryForUpdate.Name;

        QueueDomainEvent(new CountryUpdated(){ Id = Id });
        return this;
    }

    // Add Prop Methods Marker -- Deleting this comment will cause the add props utility to be incomplete
    
    protected Country() { } // For EF + Mocking
}

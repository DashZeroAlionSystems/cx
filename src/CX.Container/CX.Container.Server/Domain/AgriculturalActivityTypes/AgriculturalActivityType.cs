namespace CX.Container.Server.Domain.AgriculturalActivityTypes;

using CX.Container.Server.Domain.AgriculturalActivities;
using CX.Container.Server.Domain.AgriculturalActivityTypes.Models;
using CX.Container.Server.Domain.AgriculturalActivityTypes.DomainEvents;


public class AgriculturalActivityType : Entity<Guid>
{
    public string Name { get; private set; }

    public string Content { get; private set; }

    public IReadOnlyCollection<AgriculturalActivity> AgriculturalActivities { get; } = new List<AgriculturalActivity>();

    // Add Props Marker -- Deleting this comment will cause the add props utility to be incomplete


    public static AgriculturalActivityType Create(AgriculturalActivityTypeForCreation agriculturalActivityTypeForCreation)
    {
        var newAgriculturalActivityType = new AgriculturalActivityType();

        newAgriculturalActivityType.Name = agriculturalActivityTypeForCreation.Name;
        newAgriculturalActivityType.Content = agriculturalActivityTypeForCreation.Content;

        newAgriculturalActivityType.QueueDomainEvent(new AgriculturalActivityTypeCreated(){ AgriculturalActivityType = newAgriculturalActivityType });
        
        return newAgriculturalActivityType;
    }

    public AgriculturalActivityType Update(AgriculturalActivityTypeForUpdate agriculturalActivityTypeForUpdate)
    {
        Name = agriculturalActivityTypeForUpdate.Name;
        Content = agriculturalActivityTypeForUpdate.Content;

        QueueDomainEvent(new AgriculturalActivityTypeUpdated(){ Id = Id });
        return this;
    }

    // Add Prop Methods Marker -- Deleting this comment will cause the add props utility to be incomplete
    
    protected AgriculturalActivityType() { } // For EF + Mocking
}

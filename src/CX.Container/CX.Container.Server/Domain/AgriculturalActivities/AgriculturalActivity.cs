namespace CX.Container.Server.Domain.AgriculturalActivities;

using CX.Container.Server.Domain.AgriculturalActivityTypes;
using CX.Container.Server.Domain.Profiles;
using CX.Container.Server.Domain.AgriculturalActivities.Models;
using CX.Container.Server.Domain.AgriculturalActivities.DomainEvents;

public class AgriculturalActivity : Entity<Guid>
{
    public string Name { get; private set; }

    public string Scale { get; private set; }
    public Guid? ProfileId { get; private set; }
    public Profile Profile { get; }
    public Guid? AgriculturalActivityTypeId { get; private set; }
    public AgriculturalActivityType AgriculturalActivityType { get; private set; }

    // Add Props Marker -- Deleting this comment will cause the add props utility to be incomplete


    public static AgriculturalActivity Create(AgriculturalActivityForCreation agriculturalActivityForCreation)
    {
        var newAgriculturalActivity = new AgriculturalActivity();

        newAgriculturalActivity.ProfileId = agriculturalActivityForCreation.ProfileId;
        newAgriculturalActivity.AgriculturalActivityTypeId = agriculturalActivityForCreation.AgriculturalActivityTypeId;
        newAgriculturalActivity.Name = agriculturalActivityForCreation.Name;
        newAgriculturalActivity.Scale = agriculturalActivityForCreation.Scale;

        newAgriculturalActivity.QueueDomainEvent(new AgriculturalActivityCreated(){ AgriculturalActivity = newAgriculturalActivity });
        
        return newAgriculturalActivity;
    }

    public AgriculturalActivity Update(AgriculturalActivityForUpdate agriculturalActivityForUpdate)
    {
        AgriculturalActivityTypeId = agriculturalActivityForUpdate.AgriculturalActivityTypeId;
        Name = agriculturalActivityForUpdate.Name;
        Scale = agriculturalActivityForUpdate.Scale;

        QueueDomainEvent(new AgriculturalActivityUpdated(){ Id = Id });
        return this;
    }

    public AgriculturalActivity SetAgriculturalActivityType(AgriculturalActivityType agriculturalActivityType)
    {
        AgriculturalActivityType = agriculturalActivityType;
        return this;
    }

    // Add Prop Methods Marker -- Deleting this comment will cause the add props utility to be incomplete
    
    protected AgriculturalActivity() { } // For EF + Mocking
}

namespace CX.Container.Server.Domain.AgriculturalActivities.Models;
public sealed class AgriculturalActivityForUpdate
{
    public Guid? AgriculturalActivityTypeId { get; set; }
    public string Name { get; set; }
    public string Scale { get; set; }

}

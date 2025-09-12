namespace CX.Container.Server.Domain.AgriculturalActivities.Models;
public sealed class AgriculturalActivityForCreation
{
    public Guid? AgriculturalActivityTypeId { get; set; }
    public Guid? ProfileId { get; set; }
    public string Name { get; set; }
    public string Scale { get; set; }

}

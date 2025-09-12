namespace CX.Container.Server.Domain.AgriculturalActivities.Dtos;

/// <summary>
/// Data Transfer Object exposing the properties of an Agricultural Activity for Update.
/// </summary>
public sealed record AgriculturalActivityForUpdateDto
{
    /// <summary>
    /// Unique Identifier for the Type of Agricultural Activity of this Activity.
    /// </summary>
    public Guid? AgriculturalActivityTypeId { get; set; }
    
    /// <summary>
    /// Description of the Agricultural Activity.
    /// </summary>
    public string Name { get; set; }
    
    /// <summary>
    /// Scale of the Agricultural Activity.
    /// </summary>
    public string Scale { get; set; }

}

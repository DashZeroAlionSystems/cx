namespace CX.Container.Server.Domain.AgriculturalActivities.Dtos;

/// <summary>
/// Data Transfer Object exposing the properties of an Agricultural Activity.
/// </summary>
public sealed record AgriculturalActivityDto
{
    /// <summary>
    /// Unique Identifier
    /// </summary>
    public Guid Id { get; set; }
    
    /// <summary>
    /// Unique Identifier for the Type of Agricultural Activity
    /// </summary>
    public Guid? AgriculturalActivityTypeId { get; set; }
    
    /// <summary>
    /// Description of the Agricultural Activity
    /// </summary>
    public string Name { get; set; }
    
    /// <summary>
    /// The scale of the Agricultural Activity
    /// </summary>
    public string Scale { get; set; }

}

namespace CX.Container.Server.Domain.AgriculturalActivities.Dtos;

/// <summary>
/// Data Transfer Object exposing the properties of an Agricultural Activity for Creation.
/// </summary>
public sealed record AgriculturalActivityForCreationDto
{
    /// <summary>
    /// Unique Identifier for the Type of Agricultural Activity
    /// </summary>
    public Guid? AgriculturalActivityTypeId { get; set; }
    
    /// <summary>
    /// Unique Identifier for the Profile to which this Agricultural Activity belongs.
    /// <remarks>
    /// The profile can only be allocated on creation.
    /// </remarks>
    /// </summary>
    public Guid? ProfileId { get; set; }
    
    /// <summary>
    /// Description of the Agricultural Activity
    /// </summary>
    public string Name { get; set; }
    
    /// <summary>
    /// Scale of the Agricultural Activity
    /// </summary>
    public string Scale { get; set; }

}

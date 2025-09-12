namespace CX.Container.Server.Domain.AgriculturalActivityTypes.Dtos;

/// <summary>
/// Exposes the data transfer object for updating an existing Agricultural Activity Type.
/// </summary>
public sealed record AgriculturalActivityTypeForUpdateDto
{
    /// <summary>
    /// Name or Description of the Type of Agricultural Activity.
    /// </summary>
    public string Name { get; set; }
    
    /// <summary>
    /// Detailed information about the Type of Agricultural Activity.
    /// </summary>
    public string Content { get; set; }
}

namespace CX.Container.Server.Domain.AgriculturalActivityTypes.Dtos;

/// <summary>
/// Exposes the data transfer object for creating a new Agricultural Activity Type.
/// </summary>
public sealed record AgriculturalActivityTypeForCreationDto
{
    /// <summary>
    /// Description or Name of the Agricultural Activity Type.
    /// </summary>
    public string Name { get; set; }
    
    /// <summary>
    /// Detailed information about the Agricultural Activity Type.
    /// </summary>
    public string Content { get; set; }
}

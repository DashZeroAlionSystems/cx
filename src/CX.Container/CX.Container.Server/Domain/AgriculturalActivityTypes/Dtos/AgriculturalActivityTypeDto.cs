namespace CX.Container.Server.Domain.AgriculturalActivityTypes.Dtos;

/// <summary>
/// Data Transfer Object exposing the properties of an Agricultural Activity Type.
/// </summary>
public sealed record AgriculturalActivityTypeDto
{
    /// <summary>
    /// Agricultural Activity Type Unique Identifier.
    /// </summary>
    public Guid Id { get; set; }
    
    /// <summary>
    /// Description or Name of the Agricultural Activity Type.
    /// </summary>
    public string Name { get; set; }
    
    /// <summary>
    /// Detailed information about the Agricultural Activity Type.
    /// </summary>
    public string Content { get; set; }
}

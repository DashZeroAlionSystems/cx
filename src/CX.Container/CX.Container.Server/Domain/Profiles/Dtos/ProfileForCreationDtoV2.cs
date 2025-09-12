namespace CX.Container.Server.Domain.Profiles.Dtos;

/// <summary>
/// Data Transfer Object representing a user profile for Creation.
/// </summary>
public sealed record ProfileForCreationDtoV2
{
    /// <summary>
    /// Unique identifier for the user the profile belongs to.
    /// <remarks>
    /// The UserId is optional, if not provided the UserId will be set to the logged-in user's Id.
    /// </remarks>
    /// </summary>
    public string UserId { get; set; }
    
    /// <summary>
    /// Name of the User's Location.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Location Id
    /// </summary>
    public string LocationId { get; set; }

    /// <summary>
    /// Latitude of location
    /// </summary>
    public string Latitude { get; set; }

    /// <summary>
    /// Longitude of location
    /// </summary>
    public string Longitude { get; set; }
}

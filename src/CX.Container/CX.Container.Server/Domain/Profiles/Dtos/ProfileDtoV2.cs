namespace CX.Container.Server.Domain.Profiles.Dtos;

/// <summary>
/// Data Transfer Object representing a user profile v2.
/// </summary>
public sealed record ProfileDtoV2
{
    /// <summary>
    /// Unique identifier for the profile.
    /// </summary>
    public Guid Id { get; set; }
    
    /// <summary>
    /// Unique identifier of the user this Profile belongs to.
    /// </summary>
    public string UserId { get; set; }
    
    /// <summary>
    /// Name of the User's Profile.
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

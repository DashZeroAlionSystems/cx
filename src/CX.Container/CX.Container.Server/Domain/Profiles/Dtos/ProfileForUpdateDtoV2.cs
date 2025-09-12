namespace CX.Container.Server.Domain.Profiles.Dtos;

/// <summary>
/// Data Transfer Object representing a user profile for Update.
/// </summary>
public sealed record ProfileForUpdateDtoV2
{
    /// <summary>
    /// The name of the user's Profile.
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

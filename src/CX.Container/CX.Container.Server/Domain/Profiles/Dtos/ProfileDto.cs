namespace CX.Container.Server.Domain.Profiles.Dtos;

/// <summary>
/// Data Transfer Object representing a user profile.
/// </summary>
public sealed record ProfileDto
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
    /// Name of the User's Farm.
    /// </summary>
    public string Name { get; set; }
    
    /// <summary>
    /// Address Line 1, containing data such as the street number and street name.
    /// </summary>
    public string AddressLine1 { get; set; }
    
    /// <summary>
    /// Address Line 2, containing data such as the suburb.
    /// </summary>
    public string AddressLine2 { get; set; }
    
    /// <summary>
    /// Address Line 3, containing data such as the state.
    /// </summary>
    public string AddressLine3 { get; set; }
    
    /// <summary>
    /// The City.
    /// </summary>
    public string City { get; set; }
    
    /// <summary>
    /// The Postal or Zip code.
    /// </summary>
    public string PostalCode { get; set; }

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

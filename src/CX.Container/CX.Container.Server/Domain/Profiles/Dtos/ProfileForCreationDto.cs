namespace CX.Container.Server.Domain.Profiles.Dtos;

/// <summary>
/// Data Transfer Object representing a user profile for Creation.
/// </summary>
public sealed record ProfileForCreationDto
{
    /// <summary>
    /// Unique identifier for the user the profile belongs to.
    /// <remarks>
    /// The UserId is optional, if not provided the UserId will be set to the logged-in user's Id.
    /// </remarks>
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
    /// Address Line 2, containing data such as the Suburb.
    /// </summary>
    public string AddressLine2 { get; set; }
    
    /// <summary>
    /// Address Line 3, containing data such as the State or Province.
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

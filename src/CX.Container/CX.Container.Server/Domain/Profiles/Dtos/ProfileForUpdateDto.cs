namespace CX.Container.Server.Domain.Profiles.Dtos;

/// <summary>
/// Data Transfer Object representing a user profile for Update.
/// </summary>
public sealed record ProfileForUpdateDto
{
    /// <summary>
    /// The name of the user's farm.
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

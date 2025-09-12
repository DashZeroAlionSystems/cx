namespace CX.Container.Server.Domain.Addresses.Dtos;


/// <summary>
/// Data Transfer Object exposing the properties of an Address.
/// </summary>
public class AddressDto
{
    /// <summary>
    /// Address Line 1, such as a house number and street name.
    /// </summary>
    public string Line1 { get; set; }
    
    /// <summary>
    /// Address Line 2, such as an apartment, suite, or office number.
    /// </summary>
    public string Line2 { get; set; }
    
    /// <summary>
    /// City
    /// </summary>
    public string City { get; set; }
    
    /// <summary>
    /// State or Province
    /// </summary>
    public string State { get; set; }
    
    /// <summary>
    /// Postal or Zip Code
    /// </summary>
    public string PostalCode { get; set; }
    
    /// <summary>
    /// Country
    /// </summary>
    public string Country { get; set; }
}
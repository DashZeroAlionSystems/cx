namespace CX.Container.Server.Domain.Addresses.Dtos;
            
/// <summary>
/// Data Transfer Object exposing the properties of an Address for Creation.
/// </summary>
public class AddressForCreationDto
{
    /// <summary>
    /// Address Line 1
    /// </summary>
    public string Line1 { get; set; }
    
    /// <summary>
    /// Address Line 2
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
    /// Postal Code or Zip Code
    /// </summary>
    public string PostalCode { get; set; }
    
    /// <summary>
    /// Country
    /// </summary>
    public string Country { get; set; }
}
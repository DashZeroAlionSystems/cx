namespace CX.Container.Server.Domain.Profiles.Models;
public sealed class ProfileForUpdate
{
    public string Name { get; set; }
    public string AddressLine1 { get; set; }
    public string AddressLine2 { get; set; }
    public string AddressLine3 { get; set; }
    public string City { get; set; }
    public string PostalCode { get; set; }
    public string LocationId { get; set; }
    public string Latitude { get; set; }
    public string Longitude { get; set; }

}

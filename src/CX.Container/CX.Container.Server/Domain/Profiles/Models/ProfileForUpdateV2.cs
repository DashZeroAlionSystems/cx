namespace CX.Container.Server.Domain.Profiles.Models;
public sealed class ProfileForUpdateV2
{
    public string Name { get; set; }
    public string LocationId { get; set; }
    public string Latitude { get; set; }
    public string Longitude { get; set; }
}

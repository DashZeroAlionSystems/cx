namespace CX.Container.Server.Domain.Profiles.Models;
public sealed class ProfileForCreationV2
{
    public string UserId { get; set; }
    public string Name { get; set; }
    public string LocationId { get; set; }
    public string Latitude { get; set; }
    public string Longitude { get; set; }
}

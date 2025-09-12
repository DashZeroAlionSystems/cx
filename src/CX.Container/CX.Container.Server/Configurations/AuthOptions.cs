namespace CX.Container.Server.Configurations;

public class AuthOptions
{
    public string Audience { get; set; } = String.Empty;
    public string Authority { get; set; } = String.Empty;
    public string AuthorizationUrl { get; set; } = String.Empty;
    public string TokenUrl { get; set; } = String.Empty;
    public string ClientId { get; set; } = String.Empty;
    public string ClientSecret { get; set; } = String.Empty;
    public string ManagementApiDomain { get; set; } = String.Empty;
    public string ManagementApiAudience => $"https://{ManagementApiDomain}/api/v2/";
    public string AuthenticationApiUrl => $"https://{ManagementApiDomain}";
}
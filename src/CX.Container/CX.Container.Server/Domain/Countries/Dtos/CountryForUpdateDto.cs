namespace CX.Container.Server.Domain.Countries.Dtos;

/// <summary>
/// Data Transfer Object exposing the properties of a Country for Update.
/// </summary>
public sealed record CountryForUpdateDto
{
    /// <summary>
    /// Two-letter ISO 3166-1 alpha-2 country code
    /// </summary>
    public string CountryCode { get; set; }
    
    /// <summary>
    /// Name of the Country
    /// </summary>
    public string Name { get; set; }
}

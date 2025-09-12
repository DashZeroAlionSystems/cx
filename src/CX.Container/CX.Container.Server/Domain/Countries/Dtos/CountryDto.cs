namespace CX.Container.Server.Domain.Countries.Dtos;

/// <summary>
/// Data Transfer Object exposing the properties of a Country.
/// </summary>
public sealed record CountryDto
{
    /// <summary>
    /// Unique Identifier
    /// </summary>
    public Guid Id { get; set; }
    
    /// <summary>
    /// Two-letter ISO 3166-1 alpha-2 country code
    /// </summary>
    public string CountryCode { get; set; }
    
    /// <summary>
    /// Name of the Country
    /// </summary>
    public string Name { get; set; }
}

namespace CX.Container.Server.Domain.Preferences.Dtos;

/// <summary>
/// Data Transfer Object representing a user preference.
/// </summary>
public sealed record PreferenceDto
{
    /// <summary>
    /// Unique identifier for the preference.
    /// </summary>
    public Guid Id { get; set; }
    
    /// <summary>
    /// The key of the preference.
    /// </summary>
    public string Key { get; set; }
    
    /// <summary>
    /// The preference's value.
    /// </summary>
    public string Value { get; set; }
}

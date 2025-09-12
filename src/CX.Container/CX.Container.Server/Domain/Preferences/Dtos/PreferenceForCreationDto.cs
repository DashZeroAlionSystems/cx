namespace CX.Container.Server.Domain.Preferences.Dtos;

/// <summary>
/// Data Transfer Object representing a user preference for Creation.
/// </summary>
public sealed record PreferenceForCreationDto
{
    /// <summary>
    /// The key of the preference.
    /// </summary>
    public string Key { get; set; }
    
    /// <summary>
    /// The preference's value.
    /// </summary>
    public string Value { get; set; }
}

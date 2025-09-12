namespace CX.Container.Server.Domain.Preferences.Dtos;

/// <summary>
/// Data Transfer Object representing a user preference for Update.
/// </summary>
public sealed record PreferenceForUpdateDto
{
    /// <summary>
    /// Key of the preference.
    /// </summary>
    public string Key { get; set; }
    
    /// <summary>
    /// Value of the preference.
    /// </summary>
    public string Value { get; set; }
}
namespace CX.Container.Server.Domain.Users.Dtos;

/// <summary>
/// Data Transfer Object representing a User to be Updated.
/// </summary>
public sealed record UserForUpdateDto
{
    /// <summary>
    /// User's updated First Name.
    /// </summary>
    public string FirstName { get; set; }
    
    /// <summary>
    /// User's updated Last Name.
    /// </summary>
    public string LastName { get; set; }
    
    /// <summary>
    /// User's updated Email Address.
    /// </summary>
    public string Email { get; set; }
    
    /// <summary>
    /// User's updated Username.
    /// </summary>
    public string Username { get; set; }

}

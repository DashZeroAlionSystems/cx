namespace CX.Container.Server.Domain.Users.Dtos;

/// <summary>
/// Data Transfer Object representing a User.
/// </summary>
public sealed record UserDto
{
    /// <summary>
    /// User's Unique Identifier.
    /// </summary>
    public string Id { get; set; }
    
    /// <summary>
    /// User's First Name.
    /// </summary>
    public string FirstName { get; set; }
    
    /// <summary>
    /// User's Last Name.
    /// </summary>
    public string LastName { get; set; }
    
    /// <summary>
    /// User's Email Address.
    /// </summary>
    public string Email { get; set; }
    
    /// <summary>
    /// User's Username.
    /// </summary>
    public string Username { get; set; }
    
    public List<string> Roles { get; set; }

}

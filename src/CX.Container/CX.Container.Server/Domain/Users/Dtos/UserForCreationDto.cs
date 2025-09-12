namespace CX.Container.Server.Domain.Users.Dtos;

/// <summary>
/// Data Transfer Object representing a User to be Created.
/// </summary>
public sealed record UserForCreationDto
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
    /// Users's Email Address.
    /// </summary>
    public string Email { get; set; }
    
    /// <summary>
    /// User's Username.
    /// </summary>
    public string Username { get; set; }

}

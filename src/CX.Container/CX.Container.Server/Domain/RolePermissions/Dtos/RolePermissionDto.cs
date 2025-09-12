namespace CX.Container.Server.Domain.RolePermissions.Dtos;

/// <summary>
/// Data Transfer Object representing a Role-Permission.
/// </summary>
public sealed record RolePermissionDto
{
    /// <summary>
    /// Unique identifier for the Role-Permission.
    /// </summary>
    public Guid Id { get; set; }
    
    /// <summary>
    /// The Role
    /// </summary>
    public string Role { get; set; }
    
    /// <summary>
    /// The Role's Permission.
    /// </summary>
    public string Permission { get; set; }
}

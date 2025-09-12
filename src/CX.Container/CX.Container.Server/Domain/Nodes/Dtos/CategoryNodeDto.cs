using System.ComponentModel.DataAnnotations;

namespace CX.Container.Server.Domain.Nodes.Dtos;

/// <summary>
/// Data Transfer Object representing a Category Node and it's direct children.
/// </summary>
public sealed record CategoryNodeDto
{
    /// <summary>
    /// Unique Identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Unique Identifier linking to this Node's <see cref="Projects.Project"/>.
    /// </summary>
    public Guid ProjectId { get; set; }

    /// <summary>
    /// Unique Identifier linking to this Node's Parent Node.
    /// </summary>
    public Guid? ParentId { get; set; }
        
    /// <summary>
    /// Name of the Node.
    /// </summary>
    public string Name { get; set; }
    
    /// <summary>
    /// Indicates if this is an Asset and not a Category Node.
    /// </summary>
    public bool IsAsset { get; set; }
}
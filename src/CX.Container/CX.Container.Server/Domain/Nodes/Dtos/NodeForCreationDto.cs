using System.ComponentModel.DataAnnotations;

namespace CX.Container.Server.Domain.Nodes.Dtos;

/// <summary>
/// Data Transfer Object representing a Node to be Created.
/// </summary>
public sealed record NodeForCreationDto
{
    /// <summary>
    /// Unique Identifier linking to this Node's Source.
    /// </summary>
    public Guid? SourceId { get; set; }

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
    /// Description of the Node.
    /// </summary>
    [MaxLength(254)]
    public string Description { get; set; }

    /// <summary>
    /// Indicates if this is an Asset and not a Category Node.
    /// </summary>    
    public bool IsAsset { get; set; }

    /// <summary>
    /// Keywords of the Node.
    /// </summary>
    public string Keywords { get; set; }

    /// <summary>
    /// Tags of the Node.
    /// </summary>
    public string Tags { get; set; }
}
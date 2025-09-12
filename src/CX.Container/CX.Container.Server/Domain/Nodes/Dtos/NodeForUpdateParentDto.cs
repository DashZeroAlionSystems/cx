namespace CX.Container.Server.Domain.Nodes.Dtos;

/// <summary>
/// Data Transfer Object representing a Node's Parent Id to be Updated.
/// </summary>
public sealed record NodeForUpdateParentDto
{   
    /// <summary>
    /// Unique Identifier linking to this Node's Parent Node.
    /// </summary>
    public Guid? ParentId { get; set; }

}
namespace CX.Container.Server.Domain.Nodes.Dtos;

/// <summary>
/// Data Transfer Object representing a Node Asset.
/// </summary>
public sealed record NodeForUrlUploadDto
{
    /// <summary>
    /// Unique identifier specifying this Asset's <see cref="Nodes.Node"/>
    /// </summary>
    public Guid NodeId { get; set; }

    /// <summary>
    /// URL.
    /// </summary>
    public string Url { get; set; }
}
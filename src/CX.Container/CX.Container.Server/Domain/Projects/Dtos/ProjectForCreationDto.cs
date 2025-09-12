namespace CX.Container.Server.Domain.Projects.Dtos;

/// <summary>
/// Data Transfer Object representing a Project to be Created.
/// </summary>
public sealed record ProjectForCreationDto
{
    /// <summary>
    /// Name of the Project.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Description of the Project.
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// Thumbnail of the Project.
    /// </summary>
    public string Thumbnail { get; set; }

    /// <summary>
    /// Namespace of the Project.
    /// </summary>
    public string Namespace { get; set; }
}
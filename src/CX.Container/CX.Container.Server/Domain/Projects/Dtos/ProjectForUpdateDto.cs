namespace CX.Container.Server.Domain.Projects.Dtos;

/// <summary>
/// Data Transfer Object representing a Project to be Updated.
/// </summary>
public sealed record ProjectForUpdateDto
{
    /// <summary>
    /// New Name of the Project.
    /// </summary>
    public string Name { get; set; }
    
    /// <summary>
    /// New Description of the Project.
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// New Thumbnail of the Project.
    /// </summary>
    public string Thumbnail { get; set; }

    /// <summary>
    /// Namespace of the Project.
    /// </summary>
    public string Namespace { get; set; }

}

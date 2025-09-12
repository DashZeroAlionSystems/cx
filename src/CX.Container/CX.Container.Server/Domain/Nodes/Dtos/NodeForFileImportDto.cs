namespace CX.Container.Server.Domain.Nodes.Dtos;

/// <summary>
/// Data Transfer Object representing a Project Id and an File for importing.
/// </summary>
public sealed record NodeForFileImportDto
{    
    /// <summary>
    /// Unique identifier
    /// </summary>
    public Guid ProjectId { get; set; }

    /// <summary>
    /// Actual File.
    /// </summary>
    public IFormFile File { get; set; }

}

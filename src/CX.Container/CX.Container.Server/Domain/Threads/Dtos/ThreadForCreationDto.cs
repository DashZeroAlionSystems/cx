namespace CX.Container.Server.Domain.Threads.Dtos;

/// <summary>
/// Data Transfer Object representing a conversation/chat Thread to be Created.
/// </summary>
public sealed record ThreadForCreationDto
{
    /// <summary>
    /// Name of the Thread.
    /// </summary>
    public string Name { get; set; }

}

namespace CX.Container.Server.Domain.Threads.Dtos;

/// <summary>
/// Data Transfer Object representing a conversation/chat Thread to be Updated.
/// </summary>
public sealed record ThreadForUpdateDto
{
    /// <summary>
    /// New Name of the Thread.
    /// </summary>
    public string Name { get; set; }

}

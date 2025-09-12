namespace CX.Container.Server.Domain.Threads.Dtos;

/// <summary>
/// Data Transfer Object representing a conversation/chat Thread.
/// </summary>
public sealed record ThreadDto
{
    /// <summary>
    /// Thread's Unique Identifier.
    /// </summary>
    public Guid Id { get; set; }
    
    /// <summary>
    /// Name of the Thread.
    /// </summary>
    public string Name { get; set; }
    
    /// <summary>
    /// <c>true</c> if the Thread has Pinned messages, <c>false</c> otherwise.
    /// </summary>
    public bool HasPinnedMessages { get; set; }

    /// <summary>
    /// Last Modified Timestamp.
    /// </summary>
    public DateTime? LastModifiedOn { get; set; }
}

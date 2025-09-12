using CX.Container.Server.Domain.MessageCitations.Dtos;

namespace CX.Container.Server.Domain.Messages.Dtos;

/// <summary>
/// Message Data Transfer Object representing a message for Update.
/// </summary>
public sealed record MessageForUpdateDto
{
    /// <summary>
    /// The actual message content.
    /// </summary>
    public string Content { get; set; }
    
    /// <summary>
    /// The type of content in the message defined by <see cref="ContentTypes.ContentType"/>.
    /// </summary>
    public string ContentType { get; set; }
    
    /// <summary>
    /// The type of message to specify if the message was sent by the user or the AI. Values are defined by <see cref="MessageTypes.MessageType"/>.
    /// </summary>
    public string MessageType { get; set; }
    
    /// <summary>
    /// The type of feedback received on the message, if any. Values are defined by <see cref="FeedbackTypes.FeedbackType"/>.
    /// </summary>
    public string Feedback { get; set; }
    
    /// <summary>
    /// Indicates if the message is flagged for review by a moderator.
    /// </summary>
    public bool IsFlagged { get; set; }
    
    /// <summary>
    /// Indicates if the message is pinned as a favourite by the user.
    /// </summary>
    public bool IsPinned { get; set; }

    /// <summary>
    /// Citations for the response message
    /// </summary>
    public MessageCitationDto[] Citations { get; set; }
}

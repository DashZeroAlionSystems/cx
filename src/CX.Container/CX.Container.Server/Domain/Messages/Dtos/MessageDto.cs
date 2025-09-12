using CX.Container.Server.Domain.MessageCitations.Dtos;
using CX.Container.Server.Services;
using System.Text.Json.Serialization;

namespace CX.Container.Server.Domain.Messages.Dtos;

/// <summary>
/// Data Transfer Object representing a message sent to and received from the AI.
/// </summary>
public sealed record MessageDto
{
    /// <summary>
    /// Unique identifier for the message.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Unique identifier for the <see cref="Thread"/> to which this message belongs.
    /// </summary>
    public Guid? ThreadId { get; set; }
    
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
    public string Feedback { get; set; } = FeedbackTypes.FeedbackType.None().Value;
    
    /// <summary>
    /// Indicates if the message is flagged for review by a moderator.
    /// </summary>
    public bool IsFlagged { get; set; }
    
    /// <summary>
    /// Indicates if the message is pinned as a favourite by the user.
    /// </summary>
    public bool IsPinned { get; set; }
    
    public DateTime? LastModifiedOn { get; set; }

    /// <summary>
    /// Citations for the response message
    /// </summary>
    [JsonPropertyName("citations")]
    public MessageCitationDto[] Citations { get; set; }

    /// <summary>
    /// Factory method to create a new instance of <see cref="MessageDto"/> containing an error message.
    /// <remarks>Used by the <see cref="AiService"/> to return error messages to the client.</remarks>
    /// </summary>
    /// <param name="message">The message content.</param>
    /// <returns><see cref="MessageDto"/></returns>
    public static MessageDto ForError(string message)
    {
        return new MessageDto()
        {
            Id = Guid.Empty,
            ThreadId = null,
            Content = message,
            ContentType = ContentTypes.ContentType.PlainText().Value,
            MessageType = MessageTypes.MessageType.Error().Value
        };
    }
}
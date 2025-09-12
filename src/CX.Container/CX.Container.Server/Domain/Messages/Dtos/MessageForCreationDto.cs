using CX.Container.Server.Domain.MessageCitations.Dtos;

namespace CX.Container.Server.Domain.Messages.Dtos;

/// <summary>
/// Data Transfer Object representing a message for Creation.
/// </summary>
public sealed record MessageForCreationDto
{
    /// <summary>
    /// <see cref="Thread"/> this message belongs to.
    /// <remarks>
    /// When null, a new one will be created with the <see cref="Thread.Name"/> as the first 50 characters of the message.
    /// </remarks>
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
    /// The name of the assistant to use for the response.
    /// </summary>
    public string? ChannelName { get; set; }

    /// <summary>
    /// Citations for the response message
    /// </summary>
    public MessageCitationDto[] Citations { get; set; }  
}

using System.Text.Json.Serialization;

namespace CX.Container.Server.Domain.Messages.Dtos;

/// <summary>
/// Data Transfer Object representing a message to be sent to the AI.
/// </summary>
public class MessageForChatDto
{
    /// <summary>
    /// When <c>true</c>, the message was sent by the user. When <c>false</c>, the message was the AI's response.
    /// </summary>
    [JsonPropertyName("from_user")]
    public bool FromUser { get; set; }
    
    /// <summary>
    /// The message content.
    /// </summary>
    [JsonPropertyName("message")]
    public string Message { get; set; }
}
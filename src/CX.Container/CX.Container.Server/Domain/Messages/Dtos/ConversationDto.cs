using System.Text.Json.Serialization;

namespace CX.Container.Server.Domain.Messages.Dtos;

/// <summary>
/// Data Transfer Object representing an entire conversation (including history) with the AI.
/// </summary>
public class ConversationDto
{
    /// <summary>
    /// Previous messages sent to and received from the AI.
    /// </summary>
    [JsonPropertyName("history_data")]
    public List<MessageForChatDto> History { get; set; }
    
    /// <summary>
    /// UserId of the user who initiated the conversation.
    /// <remarks>
    /// Currently using the ThreadId of the message as the UserId.
    /// </remarks>
    /// </summary>
    [JsonPropertyName("user_id")]
    public string UserId { get; set; }
    
    /// <summary>
    /// The message sent to the AI.
    /// </summary>
    [JsonPropertyName("user_message")]
    public string Message { get; set; }
}
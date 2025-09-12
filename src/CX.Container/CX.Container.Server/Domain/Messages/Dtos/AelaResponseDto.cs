using CX.Container.Server.Domain.MessageCitations.Dtos;
using System.Text.Json.Serialization;

namespace CX.Container.Server.Domain.Messages.Dtos;

/// <summary>
/// Data Transfer Object wrapping the response from CX.Container AI's chat.
/// </summary>
public class AelaResponseDto
{
    /// <summary>
    /// The response message from CX.Container AI.
    /// </summary>
    [JsonPropertyName("message")]
    public string Message { get; set; }

    /// <summary>
    /// Citations for the response message
    /// </summary>
    [JsonPropertyName("citations")]
    public MessageCitationDto[] Citations { get; set; }
}
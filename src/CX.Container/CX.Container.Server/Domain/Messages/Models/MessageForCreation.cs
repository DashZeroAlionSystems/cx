using CX.Container.Server.Domain.MessageCitations.Dtos;

namespace CX.Container.Server.Domain.Messages.Models;

public sealed class MessageForCreation
{
    public Guid? ThreadId { get; set; }
    public string Content { get; set; }
    public string ContentType { get; set; }
    public string MessageType { get; set; }
    public string? ChannelName { get; set; }
    public MessageCitationDto[] Citations { get; set; }
}

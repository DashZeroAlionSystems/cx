namespace CX.Container.Server.Domain.MessageCitations.DomainEvents;

public sealed class MessageCitationCreated : DomainEvent
{
    public MessageCitation Citation { get; set; } 
}
            
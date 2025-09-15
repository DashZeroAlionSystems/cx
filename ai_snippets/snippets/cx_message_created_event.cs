namespace CX.Container.Server.Domain.Messages.DomainEvents;

public sealed class MessageCreated : DomainEvent
{
	public Message Message { get; set; } 
}
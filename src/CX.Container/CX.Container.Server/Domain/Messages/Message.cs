namespace CX.Container.Server.Domain.Messages;

using CX.Container.Server.Domain.Threads;
using CX.Container.Server.Domain.Messages.Models;
using CX.Container.Server.Domain.Messages.DomainEvents;
using CX.Container.Server.Domain.ContentTypes;
using CX.Container.Server.Domain.MessageTypes;
using CX.Container.Server.Domain.FeedbackTypes;
using CX.Container.Server.Domain.MessageCitations;

public class Message : Entity<Guid>
{
    public ICollection<MessageCitation> Citations { get; set; }

    public string Content { get; private set; }

    public ContentType ContentType { get; private set; }

    public MessageType MessageType { get; private set; }

    public FeedbackType Feedback { get; private set; }
    
    public Guid? ThreadId { get; private set; }
    public Thread Thread { get; private set; }
    
    public bool IsFlagged { get; private set; }
    public bool IsPinned { get; private set; }
    // Add Props Marker -- Deleting this comment will cause the add props utility to be incomplete
       

    public static Message Create(MessageForCreation messageForCreation)
    {
        var newMessage = new Message
        {
            ThreadId = messageForCreation.ThreadId,
            Content = messageForCreation.Content,
            ContentType = ContentType.Of(messageForCreation.ContentType),
            MessageType = MessageType.Of(messageForCreation.MessageType),
            Feedback = FeedbackType.None(),
            Citations = messageForCreation.Citations?.Select(x => new MessageCitation() {Url = x.Url, Name = x.Name, Type = x.Type}).ToArray()
        };

        newMessage.QueueDomainEvent(new MessageCreated() { Message = newMessage });

        return newMessage;
    }

    public Message Update(MessageForUpdate messageForUpdate)
    {
        Content = messageForUpdate.Content;
        ContentType = ContentType.Of(messageForUpdate.ContentType);
        MessageType = MessageType.Of(messageForUpdate.MessageType);
        Feedback = FeedbackType.Of(messageForUpdate.Feedback);
        IsFlagged = messageForUpdate.IsFlagged;
        IsPinned = messageForUpdate.IsPinned;

        QueueDomainEvent(new MessageUpdated() { Id = Id });
        return this;
    }

    public Message SetThread(Thread thread)
    {
        Thread = thread;
        return this;
    }

    // Add Prop Methods Marker -- Deleting this comment will cause the add props utility to be incomplete

    protected Message()
    {
    } // For EF + Mocking
}
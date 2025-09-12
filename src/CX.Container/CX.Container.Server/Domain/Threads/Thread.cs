namespace CX.Container.Server.Domain.Threads;

using CX.Container.Server.Domain.Messages;
using CX.Container.Server.Domain.Threads.Models;
using CX.Container.Server.Domain.Threads.DomainEvents;

public class Thread : Entity<Guid>
{
    public string Name { get; private set; }
    
    public bool HasPinnedMessages { get; private set; }

    private readonly List<Message> _messages = new();
    public IReadOnlyCollection<Message> Messages => _messages.AsReadOnly();

    // Add Props Marker -- Deleting this comment will cause the add props utility to be incomplete


    public static Thread Create(ThreadForCreation threadForCreation)
    {
        var newThread = new Thread();

        newThread.Name = threadForCreation.Name;

        newThread.QueueDomainEvent(new ThreadCreated(){ Thread = newThread });
        
        return newThread;
    }

    public static Thread Create(string name)
    {
        return Create(new ThreadForCreation
        {
            Name = name
        });
    }

    public Thread Update(ThreadForUpdate threadForUpdate)
    {
        Name = threadForUpdate.Name;

        QueueDomainEvent(new ThreadUpdated(){ Id = Id });
        return this;
    }

    public Thread UpdateHasPinnedMessages()
    {
        HasPinnedMessages = Messages != null && Messages.Any(m => m.IsPinned == true);

        QueueDomainEvent(new ThreadUpdated() { Id = Id });
        return this;
    }

    public Thread AddMessage(Message message)
    {
        _messages.Add(message);
        return this;
    }
    
    public Thread RemoveMessage(Message message)
    {
        _messages.RemoveAll(x => x.Id == message.Id);
        return this;
    }

    // Add Prop Methods Marker -- Deleting this comment will cause the add props utility to be incomplete
    
    protected Thread() { } // For EF + Mocking
}

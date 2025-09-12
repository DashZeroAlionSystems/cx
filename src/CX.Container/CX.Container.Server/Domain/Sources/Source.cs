namespace CX.Container.Server.Domain.Sources;

using CX.Container.Server.Domain.SourceDocuments;
using CX.Container.Server.Domain.Sources.Models;
using CX.Container.Server.Domain.Sources.DomainEvents;


public class Source : Entity<Guid>
{
    public string Name { get; private set; }

    public string Description { get; private set; }

    public int SourceDocumentCount => _messages.Count;
    private readonly List<SourceDocument> _messages = new();
    public IReadOnlyCollection<SourceDocument> SourceDocuments => _messages.AsReadOnly();

    // Add Props Marker -- Deleting this comment will cause the add props utility to be incomplete


    public static Source Create(SourceForCreation sourceForCreation)
    {
        var newSource = new Source();

        newSource.Name = sourceForCreation.Name;
        newSource.Description = sourceForCreation.Description;

        newSource.QueueDomainEvent(new SourceCreated(){ Source = newSource });
        
        return newSource;
    }

    public Source Update(SourceForUpdate sourceForUpdate)
    {
        Name = sourceForUpdate.Name;
        Description = sourceForUpdate.Description;

        QueueDomainEvent(new SourceUpdated(){ Id = Id });
        return this;
    }

    // Add Prop Methods Marker -- Deleting this comment will cause the add props utility to be incomplete
    
    protected Source() { } // For EF + Mocking
}

namespace CX.Container.Server.Domain;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;

public interface IEntity
{
    
}

public interface IHardDelete
{
    
}

public interface IDomainEvent
{
    List<DomainEvent> DomainEvents { get; }

    void QueueDomainEvent(DomainEvent @event);
}

public interface IAuditable
{
    DateTime CreatedOn { get; }
    string CreatedBy { get; }
    DateTime? LastModifiedOn { get; }
    string LastModifiedBy { get; }
    bool IsDeleted { get; }
    
    void UpdateCreationProperties(DateTime createdOn, string createdBy);
    void UpdateModifiedProperties(DateTime? lastModifiedOn, string lastModifiedBy);
    void UpdateIsDeleted(bool isDeleted);
}


public abstract class Entity<TKey> 
    : IEntity, IDomainEvent, IAuditable
    where TKey : IEquatable<TKey>
{
    [Key]
    public TKey Id { get; protected set; }
    
    public DateTime CreatedOn { get; protected set; }

    [AllowNull]
    public string? CreatedBy { get; protected set; }
    
    public DateTime? LastModifiedOn { get; protected set; }

    [AllowNull]
    public string? LastModifiedBy { get; protected set; }
    
    public bool IsDeleted { get; set; }
    
    [NotMapped]
    public List<DomainEvent> DomainEvents { get; } = new();

    public void QueueDomainEvent(DomainEvent @event)
    {
        if(!DomainEvents.Contains(@event))
            DomainEvents.Add(@event);
    }

    public void UpdateCreationProperties(DateTime createdOn, string createdBy)
    {
        CreatedOn = createdOn;
        CreatedBy = createdBy;
    }
    
    public void UpdateModifiedProperties(DateTime? lastModifiedOn, string lastModifiedBy)
    {
        LastModifiedOn = lastModifiedOn;
        LastModifiedBy = lastModifiedBy;
    }
    
    public void UpdateIsDeleted(bool isDeleted)
    {
        IsDeleted = isDeleted;
    }
}
namespace CX.Container.Server.Domain.Projects.DomainEvents;

public sealed class ProjectUpdated : DomainEvent
{
    public Guid Id { get; set; } 
}
            
namespace CX.Container.Server.Domain.Users.DomainEvents;

public sealed class UserDeleted : DomainEvent
{
    public string Id { get; init; } 
}
            
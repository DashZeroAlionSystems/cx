namespace CX.Container.Server.Domain.Users.DomainEvents;

public sealed class UserUpdated : DomainEvent
{
    public string Id { get; set; } 
}
            
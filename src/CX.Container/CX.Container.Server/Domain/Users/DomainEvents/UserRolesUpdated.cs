namespace CX.Container.Server.Domain.Users.DomainEvents;

public sealed class UserRolesUpdated : DomainEvent
{
    public string Id { get; set; }
}
            
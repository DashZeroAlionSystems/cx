namespace CX.Container.Server.Domain.Users;

using CX.Container.Server.Domain.Users.DomainEvents;
using Roles;

public class UserRole : Entity<Guid>, IHardDelete
{
    public User User { get; private set; }
    public Role Role { get; private set; }

    // Add Props Marker -- Deleting this comment will cause the add props utility to be incomplete
    

    public static UserRole Create(User user, Role role)
    {
        var newUserRole = new UserRole
        {
            User = user,
            Role = role
        };

        newUserRole.QueueDomainEvent(new UserRolesUpdated(){ Id = user.Id });
        
        return newUserRole;
    }

    // Add Prop Methods Marker -- Deleting this comment will cause the add props utility to be incomplete
    
    protected UserRole() { } // For EF + Mocking
}
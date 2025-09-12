using CX.Container.Server.Domain.Profiles;

namespace CX.Container.Server.Domain.Users;

using CX.Container.Server.Exceptions;
using CX.Container.Server.Domain.Users.DomainEvents;
using CX.Container.Server.Domain.Emails;
using CX.Container.Server.Domain.Users.Models;
using Roles;
using System.Text.Json.Serialization;
using System.Runtime.Serialization;

public class User : Entity<string>, IHardDelete
{
    public string FirstName { get; private set; }

    public string LastName { get; private set; }

    public Email Email { get; private set; }

    public string Username { get; private set; }

    [JsonIgnore]
    [IgnoreDataMember]
    public ICollection<UserRole> Roles { get; private set; } = new List<UserRole>();
    
    public ICollection<Profile> Profiles { get; private set; } = new List<Profile>();

    // Add Props Marker -- Deleting this comment will cause the add props utility to be incomplete


    public static User Create(UserForCreation userForCreation)
    {
        ValidationException.ThrowWhenNullOrWhitespace(userForCreation.Id, "Please provide an identifier.");

        var newUser = new User
        {
            Id = userForCreation.Id,
            FirstName = userForCreation.FirstName,
            LastName = userForCreation.LastName,
            Email = Email.Of(userForCreation.Email),
            Username = userForCreation.Username
        };

        newUser.QueueDomainEvent(new UserCreated(){ User = newUser });
        
        return newUser;
    }
    
    public void Delete()
    {
        QueueDomainEvent(new UserDeleted { Id = Id });
    }

    public User Update(UserForUpdate userForUpdate)
    {
        FirstName = userForUpdate.FirstName;
        LastName = userForUpdate.LastName;
        Email = Email.Of(userForUpdate.Email);
        Username = userForUpdate.Username;

        QueueDomainEvent(new UserUpdated(){ Id = Id });
        return this;
    }

    // Add Prop Methods Marker -- Deleting this comment will cause the add props utility to be incomplete

    public UserRole AddRole(Role role)
    {
        var newList = Roles.ToList();
        var userRole = UserRole.Create(this, role);
        newList.Add(userRole);
        UpdateRoles(newList);
        return userRole;
    }

    public UserRole RemoveRole(Role role)
    {
        var newList = Roles.ToList();
        var roleToRemove = Roles.FirstOrDefault(x => x.Role == role);
        newList.Remove(roleToRemove);
        UpdateRoles(newList);
        return roleToRemove;
    }

    private void UpdateRoles(IList<UserRole> updates)
    {
        var additions = updates.Where(userRole => Roles.All(x => x.Role != userRole.Role)).ToList();
        var removals = Roles.Where(userRole => updates.All(x => x.Role != userRole.Role)).ToList();
    
        var newList = Roles.ToList();
        removals.ForEach(toRemove => newList.Remove(toRemove));
        additions.ForEach(newRole => newList.Add(newRole));
        Roles = newList;
        QueueDomainEvent(new UserRolesUpdated(){ Id = Id});
    }
    
    protected User() { } // For EF + Mocking
}
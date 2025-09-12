namespace CX.Container.Server.Domain.Roles;

using Exceptions;
using Ardalis.SmartEnum;

// ReSharper disable once ClassNeverInstantiated.Global

/// <summary>
/// Constants for the different Roles.
/// </summary>
public sealed class RoleNames
{
    /// <summary>
    /// Normal System User.
    /// </summary>
    public const string User = "User";
    
    /// <summary>
    /// Super User with Administrative privileges.
    /// </summary>
    public const string SuperAdmin = "Super Admin";
    
    /// <summary>
    /// User with no Roles, and as such no access to any resources.
    /// </summary>
    public const string Restricted = "Restricted";
}

/// <summary>
/// <para>Value Object representing a Role.</para>
/// <para>Values defined by <see cref="RoleEnum"/></para>
/// </summary>
public class Role : ValueObject
{
    private readonly RoleEnum _role;
    
    /// <summary>
    /// The role's name.
    /// </summary>
    /// <exception cref="InvalidSmartEnumPropertyName">When attempting to assign an unsupported <see cref="RoleEnum"/> value.</exception>
    public string Value
    {
        get => _role.Name;
        private init
        {
            if (!RoleEnum.TryFromName(value, true, out var parsed))
                throw new InvalidSmartEnumPropertyName(nameof(Value), value);

            _role = parsed;
        }
    }
    
    private Role(string value)
    {
        Value = value;
    }
    
    /// <summary>
    /// Creates a new Role from a string value.
    /// </summary>
    /// <param name="value"><see cref="string"/> representing one of the <see cref="RoleEnum"/>s</param>
    /// <returns><see cref="Role"/></returns>
    public static Role Of(string value) => new(value);
    
    /// <summary>
    /// Implicitly converts a <see cref="Role"/> to a <see cref="string"/>.
    /// </summary>
    /// <param name="value"><see cref="Role"/> instance to convert.</param>
    /// <returns><see cref="string"/></returns>
    public static implicit operator string(Role value) => value.Value;
    
    /// <summary>
    /// List all <see cref="Role"/>s as <see cref="string"/>s.
    /// </summary>
    /// <returns><see cref="List{T}"/> of <see cref="string"/></returns>
    public static List<string> ListNames() => RoleEnum.List.Select(x => x.Name).ToList();

    /// <summary>
    /// Normal user with access to threads and chats.
    /// </summary>
    /// <returns>User Role</returns>
    public static Role User() => new(RoleEnum.User.Name);
    
    /// <summary>
    /// Super user with administrative privileges that can access the admin panel.
    /// </summary>
    /// <returns>Super Admin Role</returns>
    public static Role SuperAdmin() => new(RoleEnum.SuperAdmin.Name);
    
    /// <summary>
    /// Restricted user having no Roles, and as such no access to any resources.
    /// <remarks>Mostly used for testing purposes.</remarks>
    /// </summary>
    /// <returns>Restricted Role</returns>
    public static Role Restricted() => new(RoleEnum.Restricted.Name);

    protected Role() { } // EF Core
}

public abstract class RoleEnum : SmartEnum<RoleEnum>
{
    public static readonly RoleEnum User = new UserType();
    public static readonly RoleEnum SuperAdmin = new SuperAdminType();
    public static readonly RoleEnum Restricted = new RestrictedType();

    protected RoleEnum(string name, int value) : base(name, value)
    {
    }
    
    private class UserType : RoleEnum
    {
        public UserType() : base(RoleNames.User, 0)
        {
        }
    }

    private class SuperAdminType : RoleEnum
    {
        public SuperAdminType() : base(RoleNames.SuperAdmin, 1)
        {
        }
    }
    
    private class RestrictedType : RoleEnum
    {
        public RestrictedType() : base(RoleNames.Restricted, 2)
        {
        }
    }
}
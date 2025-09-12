using CX.Container.Server.Domain.Roles;

namespace CX.Container.Server.Services;

using System.Security.Claims;

public interface ICurrentUserService : IAelaServerScopedService
{
    ClaimsPrincipal? User { get; }
    string? UserId { get; }
    string? Email { get; }
    string? FirstName { get; }
    string? LastName { get; }
    string? Username { get; }
    string? ClientId { get; }
    bool IsMachine { get; }
    Role Role { get; }
}

public sealed class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    
    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;
    public string? UserId => User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? User?.FindFirstValue("sub");
    public string? Email => User?.FindFirstValue(ClaimTypes.Email);
    public string? FirstName => User?.FindFirstValue(ClaimTypes.GivenName);
    public string? LastName => User?.FindFirstValue(ClaimTypes.Surname);
    public string? Username => User
        ?.Claims
        ?.FirstOrDefault(x => x.Type is "preferred_username" or "username")
        ?.Value;
    public string? ClientId => User
        ?.Claims
        ?.FirstOrDefault(x => x.Type is "client_id" or "clientId")
        ?.Value;
    public bool IsMachine => ClientId != null;
    public Role Role => User?.FindFirstValue(ClaimTypes.Role) switch
    {
        RoleNames.SuperAdmin => Role.SuperAdmin(),
        RoleNames.Restricted => Role.Restricted(),
        _ => Role.User()
    };
    
    }
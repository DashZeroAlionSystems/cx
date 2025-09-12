using CX.Container.Server.Resources;
using Serilog;
using ZiggyCreatures.Caching.Fusion;

namespace CX.Container.Server.Services;

using Domain.Roles;
using Domain.Users.Dtos;
using Domain.Users.Features;
using Domain.Users.Services;
using CX.Container.Server.Domain.RolePermissions.Services;
using Domain;
using Exceptions;
using HeimGuard;
using MediatR;
using Microsoft.EntityFrameworkCore;

public sealed class UserPolicyHandler : IUserPolicyHandler
{
    private readonly IRolePermissionRepository _rolePermissionRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUserRepository _userRepository;
    private readonly IMediator _mediator;
    private readonly IFusionCache _authCache;

    public UserPolicyHandler(
        IRolePermissionRepository rolePermissionRepository,
        ICurrentUserService currentUserService,
        IUserRepository userRepository,
        IMediator mediator,
        IFusionCacheProvider cacheProvider)
    {
        _rolePermissionRepository = rolePermissionRepository;
        _currentUserService = currentUserService;
        _userRepository = userRepository;
        _mediator = mediator;
        _authCache = cacheProvider.GetCache(Consts.Cache.Auth.Name);
    }

    public async Task<IEnumerable<string>> GetUserPermissions()
    {
        var key = Consts.Cache.Auth.UserPermissionKey(_currentUserService.UserId);
       
        var cachedPermissions = await _authCache.TryGetAsync<IEnumerable<string>>(key);
        if (cachedPermissions.HasValue) return cachedPermissions.Value;
        
        var roles = await GetRoles();

        // super admins can do everything
        if (roles.Contains(Role.SuperAdmin().Value))
        {
            await _authCache.SetAsync(key, Permissions.List());
            return Permissions.List();
        }

        var permissions = await _rolePermissionRepository.Query()
            .Where(rp => roles.Contains(rp.Role))
            .Select(rp => rp.Permission)
            .Distinct()
            .ToArrayAsync();

        if (permissions.Length > 0)
        {
            await _authCache.SetAsync(key, permissions);
        }
        
        return permissions;
    }       
    
    public async Task<bool> HasPermission(string permission)
    {
        if (permission.Contains(Permissions.ClearPermissionCache))
        {
            RefreshCacheAsync(permission);
            return false;
        }            

        var permissions = await GetUserPermissions();
        
        return permissions.Any(x => x == permission);
    }

    private void RefreshCacheAsync(string permission)
    {
        var userId = permission.Replace(Permissions.ClearPermissionCache, "");
        var key = Consts.Cache.Auth.UserPermissionKey(userId);

        _authCache.Expire(key);
    }

    private async Task<string[]> GetRoles()
    {
        var claimsPrincipal = _currentUserService.User;
        if (claimsPrincipal == null) throw new ArgumentNullException(nameof(claimsPrincipal));
        
        var nameIdentifier = _currentUserService.UserId;
        if (string.IsNullOrWhiteSpace(nameIdentifier))
        {
            //_logger.LogError("NameIdentifier not found! The principal is {@ClaimsPrincipal}", claimsPrincipal);
            throw new ForbiddenAccessException("No NameIdentifier found.");
        }

        if (!_userRepository.HasUsers())
            await SeedUser(nameIdentifier, Role.SuperAdmin());          // seed super admin if no users exist
        else if (!_userRepository.HasUser(nameIdentifier))
            await SeedUser(nameIdentifier, _currentUserService.Role);   // seed new user based on the role picked up from the JWT

        var roles = _userRepository.GetRolesByUserIdentifier(nameIdentifier).ToArray(); 

        if (roles.Length == 0) throw new NoRolesAssignedException();

        return roles;
    }

    private async Task SeedUser(string userId, Role role)
    {
        var rootUser = new UserForCreationDto()
        {
            Username = _currentUserService.Username,
            Email = _currentUserService.Email,
            FirstName = _currentUserService.FirstName,
            LastName = _currentUserService.LastName,
            Id = userId
        };

        var userCommand = new AddUser.Command(rootUser, SkipPermissions: true);
        var createdUser = await _mediator.Send(userCommand);

        var roleCommand = new AddUserRole.Command(createdUser.Id, role.Value, SkipPermissions: true);
        await _mediator.Send(roleCommand);
    }
    
    
}
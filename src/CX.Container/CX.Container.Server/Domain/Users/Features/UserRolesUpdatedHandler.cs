using CX.Container.Server.Domain.Users.DomainEvents;
using CX.Container.Server.Resources;
using MediatR;
using ZiggyCreatures.Caching.Fusion;

namespace CX.Container.Server.Domain.Users.Features;

public class UserRolesUpdatedHandler : INotificationHandler<UserRolesUpdated>
{
    private readonly ILogger<UserRolesUpdatedHandler> _logger;
    private readonly IFusionCache _authCache;

    public UserRolesUpdatedHandler(
        ILogger<UserRolesUpdatedHandler> logger,
        IFusionCacheProvider cacheProvider)
    {
        _logger = logger;
        _authCache = cacheProvider.GetCache(Consts.Cache.Auth.Name);
    }

    public async Task Handle(UserRolesUpdated notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Clearing User Permission cache for {UserIdentifier}", notification.Id);

        await _authCache.ExpireAsync(Consts.Cache.Auth.UserPermissionKey(notification.Id), null, cancellationToken);
    }
}
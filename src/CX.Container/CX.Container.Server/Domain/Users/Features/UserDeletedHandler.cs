using CX.Container.Server.Domain.Users.DomainEvents;
using CX.Container.Server.Resources;
using MediatR;
using ZiggyCreatures.Caching.Fusion;

namespace CX.Container.Server.Domain.Users.Features;

public class UserDeletedHandler : INotificationHandler<UserDeleted>
{
    private readonly ILogger<UserDeletedHandler> _logger;
    private readonly IFusionCache _authCache;

    public UserDeletedHandler(
        ILogger<UserDeletedHandler> logger,
        IFusionCacheProvider cacheProvider)
    {
        _logger = logger;
        _authCache = cacheProvider.GetCache(Consts.Cache.Auth.Name);
    }

    public Task Handle(UserDeleted notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Clearing User Permission cache for Deleted User {UserIdentifier}", notification.Id);

        _authCache.RemoveAsync(Consts.Cache.Auth.UserPermissionKey(notification.Id), null, cancellationToken);

        return Task.CompletedTask;
    }
    
}
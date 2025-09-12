using CX.Container.Server.Services.Auth0;


namespace CX.Container.Server.Domain.Users.Features;

public class RemoveAuth0UserJob
{
    readonly IAuth0Service _auth0Service;
    readonly ILogger<RemoveAuth0UserJob> _logger;

    public RemoveAuth0UserJob(
        IAuth0Service auth0Service,
        ILogger<RemoveAuth0UserJob> logger)
    {
        _auth0Service = auth0Service;
        _logger = logger;
    }

    public string JobName => nameof(RemoveAuth0UserJob);

    
    public async Task DeleteUser(string userId, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("[{JobName}] Deleting user {UserId} from Auth0", JobName, userId);

            await _auth0Service.DeleteUser(userId, cancellationToken);

            _logger.LogInformation("[{JobName}] Deleted user {UserId} from Auth0", JobName, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{JobName}] Failed to delete User {UserId} from Auth0", JobName, userId);
        }
    }
}
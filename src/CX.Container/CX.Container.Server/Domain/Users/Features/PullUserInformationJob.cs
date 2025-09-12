using CX.Container.Server.Domain.Users.Services;
using CX.Container.Server.Services;
using CX.Container.Server.Services.Auth0;


namespace CX.Container.Server.Domain.Users.Features;

public class PullUserInformationJob
{
    readonly IAuth0Service _auth0Service;
    readonly IUnitOfWork _unitOfWork;
    readonly IUserRepository _userRepository;
    readonly IUserQuery _userQuery;
    readonly ILogger<PullUserInformationJob> _logger;

    readonly IAuth0MappingService _auth0MappingService;

    public PullUserInformationJob(
        ILogger<PullUserInformationJob> logger,
        IUserRepository userRepository,
        IUserQuery userQuery,
        IUnitOfWork unitOfWork,
        IAuth0Service auth0Service, 
        IAuth0MappingService auth0MappingService)
    {
        _logger = logger;
        _userRepository = userRepository;
        _userQuery = userQuery;
        _unitOfWork = unitOfWork;
        _auth0Service = auth0Service;
        _auth0MappingService = auth0MappingService;
    }

    private string JobName => nameof(PullUserInformationJob);
    
    
  public async Task Handle(CancellationToken cancellationToken)
    {
        _logger.LogInformation("[{JobName}] Starting...", JobName);

        try
        {
            await foreach (var userId in _userQuery.GetIncompleteUsersIds().WithCancellation(cancellationToken))
            {
                await UpdateUserInformation(userId, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{JobName}] Failed to execute", JobName);
        }
        finally
        {
            _logger.LogInformation("[{JobName}] Finished", JobName);
        }
    }
    
    private async Task UpdateUserInformation(string userId, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("[{JobName}] Retrieving Information for User {UserId}", JobName, userId);

            var iamUser = await _auth0Service.GetUser(userId, cancellationToken);
            if (iamUser == null)
            {
                _logger.LogWarning("[{JobName}] User {UserId} not found in Auth0", JobName, userId);
                return;
            }
        
            cancellationToken.ThrowIfCancellationRequested();
            
            var userForUpdate = _auth0MappingService.MapUserToUserForUpdate(iamUser);
            var user = await _userRepository.GetById(userId, withTracking: true, cancellationToken);
            
            user.Update(userForUpdate);
            _userRepository.Update(user);
            await _unitOfWork.CommitChanges(cancellationToken);

            _logger.LogInformation("[{JobName}] Finished adding information to User {@User}", JobName, user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{JobName}] Failed to update information for User {UserId}", JobName, userId);
        }
       
    }
}
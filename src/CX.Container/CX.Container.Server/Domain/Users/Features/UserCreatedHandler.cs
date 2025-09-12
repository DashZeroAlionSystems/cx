using CX.Container.Server.Domain.Users.DomainEvents;
using CX.Container.Server.Domain.Users.Services;
using CX.Container.Server.Services.Auth0;
using MediatR;

namespace CX.Container.Server.Domain.Users.Features;

public class UserCreatedHandler : INotificationHandler<UserCreated>
{
    private readonly ILogger<UserCreatedHandler> _logger;
    private readonly IAuth0Service _auth0Service;
    private readonly IUserRepository _userRepository;
    private readonly IAuth0MappingService _auth0MappingService;

    public UserCreatedHandler(
        ILogger<UserCreatedHandler> logger,
        IAuth0Service auth0Service,
        IUserRepository userRepository,
        IAuth0MappingService auth0MappingService)
    {
        _logger = logger;
        _auth0Service = auth0Service;
        _userRepository = userRepository;
        _auth0MappingService = auth0MappingService;
    }
    
    public async Task Handle(
        UserCreated notification,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Retrieving Information for new User {UserId}", notification.User.Id);

            var iamUser = await _auth0Service.GetUser(notification.User.Id, cancellationToken);

            var userForUpdate = _auth0MappingService.MapUserToUserForUpdate(iamUser);
            
            var user = notification.User.Update(userForUpdate);
            
            _userRepository.Update(user);
            
            _logger.LogInformation("Finished adding information to User {@User}", user);

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user information for {UserId}", notification.User.Id);
        }
    }
}
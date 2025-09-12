using CX.Container.Server.Configurations;
using Auth0.ManagementApi.Models;
using Microsoft.Extensions.Options;

namespace CX.Container.Server.Services.Auth0;


public interface IAuth0Service
{
    Task<User> GetUser(string userId, CancellationToken cancellationToken = default);
    ValueTask DeleteUser(string userId, CancellationToken cancellationToken = default);
}

public sealed class Auth0Service : IAuth0Service
{
    private readonly ILogger<Auth0Service> _logger;
    private readonly IOptionsMonitor<AuthOptions> _options;
    private readonly IAuth0ClientFactory _clientFactory;
    
    public Auth0Service(
        IOptionsMonitor<AuthOptions> options, 
        ILogger<Auth0Service> logger, 
        IAuth0ClientFactory clientFactory)
    {
        _options = options;
        _logger = logger;
        _clientFactory = clientFactory;
    }
    
    public async Task<User> GetUser(string userId, CancellationToken cancellationToken = default)
    {
        var api = await _clientFactory.GetManagementClient(cancellationToken);
        
        return await api.Users.GetAsync(userId, includeFields: true, cancellationToken: cancellationToken);
    }
    
    public async ValueTask DeleteUser(string userId, CancellationToken cancellationToken = default)
    {
        var api = await _clientFactory.GetManagementClient(cancellationToken);
        
        await api.Users.DeleteAsync(userId);
    }
}
using System.IdentityModel.Tokens.Jwt;
using CX.Container.Server.Configurations;
using Auth0.AuthenticationApi;
using Auth0.AuthenticationApi.Models;
using Auth0.ManagementApi;
using Microsoft.Extensions.Options;

namespace CX.Container.Server.Services.Auth0;

public interface IAuth0ClientFactory
{
    Task<ManagementApiClient> GetManagementClient(CancellationToken cancellationToken = default);
}

public sealed class Auth0ClientFactory : IAuth0ClientFactory
{
    private readonly ILogger<Auth0ClientFactory> _logger;
    private readonly IOptionsMonitor<AuthOptions> _options;
    private readonly TimeProvider _clock;
    private AccessToken _accessToken;
    private ManagementApiClient _managementClient;
        
    private readonly SemaphoreSlim _lock = new(1, 1);

    public Auth0ClientFactory(
        ILogger<Auth0ClientFactory> logger, 
        IOptionsMonitor<AuthOptions> options,
        TimeProvider clock)
    {
        _logger = logger;
        _options = options;
        _clock = clock;
    }

    private ClientCredentialsTokenRequest CreateAuthTokenRequest() => new()
    {
        ClientId = _options.CurrentValue.ClientId,
        ClientSecret = _options.CurrentValue.ClientSecret,
        Audience = _options.CurrentValue.ManagementApiAudience
    };

    private async Task<AccessTokenResponse> GetApiAccessToken(CancellationToken cancellationToken = default)
    {
        if (_accessToken?.IsValid(_clock) ?? false)
        {
            _logger.LogDebug("Using cached access token that expires at {ExpiresAt}", _accessToken.RefreshAt);
            return _accessToken.Token;
        }

        _logger.LogDebug("Fetching new Management API Access Token");
        var request = CreateAuthTokenRequest();

        var authenticationApiClient = new AuthenticationApiClient(_options.CurrentValue.ManagementApiDomain);
        _logger.LogDebug("Requesting new Management API Access Token from {Url}", _options.CurrentValue.ManagementApiDomain);
        
        try
        {
            var token = await authenticationApiClient.GetTokenAsync(request, cancellationToken);
            _accessToken = new AccessToken(token);

            _logger.LogDebug("Successfully obtained new Management API Access Token");
            return token;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to obtain the Management API Access Token");
            throw;
        }
    }

    public async Task<ManagementApiClient> GetManagementClient(CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (_managementClient != null && (_accessToken?.IsValid(_clock) ?? false))
            {
                return _managementClient;
            }
                
            var token = await GetApiAccessToken(cancellationToken);
            _managementClient = new ManagementApiClient(token.AccessToken, _options.CurrentValue.ManagementApiDomain);

            return _managementClient;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create management API client");
            throw;
        }
        finally
        {
            _lock.Release();
        }
    }

    private sealed class AccessToken
    {
        public AccessTokenResponse Token { get; }
        public DateTime RefreshAt { get; }

        public AccessToken(AccessTokenResponse token)
        {
            Token = token;
            var jwtSecurityToken = new JwtSecurityToken(token.AccessToken);
            RefreshAt = jwtSecurityToken.ValidTo.AddMinutes(-10);
        }

        public bool IsValid(TimeProvider clock) => RefreshAt > clock.GetUtcNow().UtcDateTime;
    }
}
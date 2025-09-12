using CX.Container.Server.Domain;
using CX.Engine.Common.ACL;
using CX.Engine.SharedOptions;

namespace CX.Container.Server.Extensions.Services;

public static class ApiKeyExtensions
{
    public static bool HasValidApiKeyAndSecret(this HttpRequest request, StructuredDataOptions structuredDataOptions) =>
        request.Headers.TryGetValue("x-api-key", out var providedApiKey) &&
        request.Headers.TryGetValue("x-api-secret", out var providedApiSecret) &&
        providedApiKey == structuredDataOptions.ApiKey && providedApiSecret == structuredDataOptions.ApiSecret;

    public static bool ApiKeyIsAllowed(this HttpRequest request, ACLService aclService, string permission) =>
        request.Headers.TryGetValue("x-api-key", out var providedApiKey) &&
        aclService.IsAllowed(providedApiKey, permission);

    public static Task<bool> HasPermissionOrApiKeyAsync(this HttpContext context, StructuredDataOptions structuredDataOptions, ACLService aclService, params string[] oneOfPermissions)
    {
        foreach (var permission in oneOfPermissions)
        {
            // Authentication is now handled by policy-based authorization
            // We assume the user is authenticated if they reach this point
            if (context.User.Identity?.IsAuthenticated ?? false)
                return Task.FromResult(true);
            
            if (context.Request.HasValidApiKeyAndSecret(structuredDataOptions))
                return Task.FromResult(true);
            
            if (context.Request.ApiKeyIsAllowed(aclService, permission))
                return Task.FromResult(true);
        }

        return Task.FromResult(false);
    }
}
namespace CX.Container.Server.Middleware;

public class JwtBearerLoggingMiddleware
{
    private readonly RequestDelegate _next;

    public JwtBearerLoggingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(
        HttpContext context,
        IJwtDecoder jwtDecoder,
        ILogger<JwtBearerLoggingMiddleware> logger)
    {
        await _next(context);

        if (context.Response.StatusCode == 401) // Unauthorized access
        {
            LogFailedAuthentication(context, jwtDecoder, logger);
        }
    }

    private void LogFailedAuthentication(
        HttpContext context,
        IJwtDecoder jwtDecoder,
        ILogger logger)
    {
        var token = context.Request.Headers.Authorization.ToString();

        if (token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            token = token.Substring("Bearer ".Length).Trim();
        }

        if (string.IsNullOrEmpty(token)) return;

        var tokenData = jwtDecoder.DecodeToken(token);

        // Use Serilog here
        logger.LogError("Authentication failed. Token: {@TokenData}", tokenData);
    }
}
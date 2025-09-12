using CX.Container.Server.Extensions.Application;

namespace CX.Container.Server.Middleware;

using System;

public interface IJwtDecoder
{
    JwtDecoder.DecodedToken DecodeToken(string token);
}

public class JwtDecoder : IJwtDecoder
{
    private readonly ILogger<JwtDecoder> _logger;

    public record DecodedToken(string Header, string Payload);

    public JwtDecoder(ILogger<JwtDecoder> logger)
    {
        _logger = logger;
    }
    
    public DecodedToken DecodeToken(string token)
    {
        var parts = token.Split('.');
        if (parts.Length != 3)
        {
            _logger.LogWarning("Invalid JWT token format. [{JwtToken}]", token);
            return null;
        }

        try
        {
            var header = parts[0].AsSpan().Base64UrlDecode();
            var payload = parts[1].AsSpan().Base64UrlDecode();
            return new DecodedToken(header, payload);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Invalid JWT token format. [{JwtToken}]", token);
            return null;
        }
    }
}

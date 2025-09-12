using JetBrains.Annotations;
using Microsoft.Extensions.Logging;

namespace CX.Engine.Common.LuaScripting;

public class LuaLogger
{
    private readonly ILogger _logger;

    public LuaLogger([NotNull] ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    public void LogDebug(string message) => _logger.LogDebug(message);
    public void LogInformation(string message) => _logger.LogInformation(message);
    public void LogWarning(string message) => _logger.LogWarning(message);
    public void LogError(string message) => _logger.LogError(message);
    public void LogCritical(string message) => _logger.LogCritical(message);
    
    public static LuaLogger For([NotNull] ILogger logger) => new(logger);
}
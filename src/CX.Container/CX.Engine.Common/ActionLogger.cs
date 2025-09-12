using Microsoft.Extensions.Logging;

namespace CX.Engine.Common;

public class ActionLogger : ILogger, IDisposable
{
    private readonly Action<string> _action;
    private readonly string _categoryName;

    public bool LogRaw;

    public ActionLogger(Action<string> action, string categoryName = null, bool logRaw = true)
    {
        LogRaw = true;
        _action = action ?? throw new ArgumentNullException(nameof(action));
        _categoryName = categoryName;
    }
    
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
    {
        // Check if the log level is enabled
        if (!IsEnabled(logLevel))
            return;

        if (formatter == null)
            throw new ArgumentNullException(nameof(formatter));

        var logMessage = formatter(state, exception);

        if (LogRaw)
            _action(logMessage);
        else if (!string.IsNullOrWhiteSpace(_categoryName))
            _action($"{logLevel.ToString()} - {_categoryName} - {logMessage}");
        else
            _action($"{logLevel.ToString()} - {logMessage}");
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        // Here you might want to filter based on the log level
        // For example, only log warnings or above:
        // return logLevel >= LogLevel.Warning;
        return true; // Log all levels for now
    }

    public IDisposable BeginScope<TState>(TState state) where TState : notnull
    {
        // No operation scope
        return this;
    }

    public void Dispose()
    {
        // Dispose resources if needed
    }

    public static implicit operator ActionLogger(Action<string> action) => new ActionLogger(action);
}
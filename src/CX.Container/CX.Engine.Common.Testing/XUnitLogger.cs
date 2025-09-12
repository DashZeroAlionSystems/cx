namespace CX.Engine.Common.Testing;

public class XunitLogger : ILogger, IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly string _categoryName;

    public XunitLogger(ITestOutputHelper output, string categoryName)
    {
        _output = output;
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
        _output.WriteLine($"{logLevel.ToString()} - {_categoryName} - {logMessage}");
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
}
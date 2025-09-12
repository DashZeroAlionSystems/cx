namespace CX.Engine.Common.Testing;

public class XunitLoggerProvider : ILoggerProvider
{
    private readonly ITestOutputHelper _output;

    public XunitLoggerProvider(ITestOutputHelper output)
    {
        _output = output;
    }

    public ILogger CreateLogger(string categoryName)
    {
        // Pass the category name to the logger
        return new XunitLogger(_output, categoryName);
    }

    public void Dispose()
    {
        // Dispose resources if needed
    }
}
using CX.Engine.ChatAgents;
using CX.Engine.Common;
using Microsoft.Extensions.Logging;

namespace CX.Engine.Assistants.SchemaSwap;

public class SchemaSwapper
{
    private SchemaSwapperOptions _options;
    private SchemaBase _schema;
    private readonly ILogger _logger;
    private readonly IServiceProvider _sp;
    private readonly SemaphoreSlim _slimLock = new(1, 1);

    public SchemaSwapper(SchemaSwapperOptions options, IServiceProvider sp,
        ILogger logger)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _sp = sp ?? throw new ArgumentNullException(nameof(sp));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void UpdateOptions(SchemaSwapperOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }
    
    public async Task SetAndWarmupSchemaAsync(SchemaBase schema)
    {
        if (_schema?.Equals(schema) == true)
            return;

        using var _ = await _slimLock.UseAsync();

        if (string.IsNullOrWhiteSpace(_options.AgentName))
            throw new ArgumentNullException($"{nameof(SchemaSwapperOptions)}.AgentName is null or empty");

        if (_schema?.Equals(schema) == true)
            return;

        var agent = _sp.GetRequiredNamedService<IChatAgent>(_options.AgentName) ??
                    throw new ArgumentNullException(
                        $"{nameof(SchemaSwapper)}.StartWarmup(): {_options.AgentName} could not be found");

        TimeSpan completionTime = TimeSpan.Zero; // Initialize with Zero instead of MaxValue
        var req = agent.GetRequest("warmup");
        req.SetResponseSchema(schema);
        req.MaxDelay = TimeSpan.FromSeconds(16);
        req.MinDelay = TimeSpan.FromSeconds(2);

        try
        {
            do
            {
                var response = await agent.RequestAsync(req);
                completionTime = response.ResponseTime;
                _logger.LogDebug("Warmup iteration completed in {CompletionTime}", completionTime);
            } while (completionTime > _options.CompletionThreshold);

            _schema = schema;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during schema warmup");
            throw;
        }
    }
}

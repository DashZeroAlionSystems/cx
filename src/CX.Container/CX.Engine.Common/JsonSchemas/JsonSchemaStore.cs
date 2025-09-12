using System.Text.Json;
using CX.Engine.Common.Stores.Json;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CX.Engine.Common.JsonSchemas;

public class JsonSchemaStore : IDisposable
{
    private readonly ILogger _logger;
    private readonly IServiceProvider _sp;
    private readonly IDisposable _optionsMonitorChangeDisposable;

    public class OptionsSnapshot
    {
        public JsonSchemaStoreOptions Options;
        public JsonStore JsonStore;
    }

    private OptionsSnapshot _snapshot;
    public OptionsSnapshot Snapshot => _snapshot;

    private void UpdateSnapshot(JsonSchemaStoreOptions options)
    {
        var ss = new OptionsSnapshot()
        {
            Options = options
        };
        ss.JsonStore = new(JsonSchemaStoreDI.ConfigurationTableForSchemas, 50, options.PostgreSQLClientName, _sp);
        _snapshot = ss;
    }

    public JsonSchemaStore(IOptionsMonitor<JsonSchemaStoreOptions> options, [NotNull] IServiceProvider sp, [NotNull] ILogger<JsonSchemaStore> logger)
    {
        _sp = sp ?? throw new ArgumentNullException(nameof(sp));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _optionsMonitorChangeDisposable = options.Snapshot(() => _snapshot?.Options, UpdateSnapshot, _logger, sp);
    }
    
    public async Task<JsonElement?> GetAsync(string schemaName) => await _snapshot.JsonStore.GetAsync<JsonElement?>(schemaName);
    public async Task SetAsync(string schemaName, JsonElement? schema) => await _snapshot.JsonStore.SetAsync(schemaName, schema);
    public async Task DeleteAsync(string schemaName) => await _snapshot.JsonStore.DeleteAsync(schemaName);
    public async Task ClearAsync() => await _snapshot.JsonStore.ClearAsync();

    public void Dispose()
    {
        _optionsMonitorChangeDisposable?.Dispose();
    }
}
using CX.Engine.Common.PostgreSQL;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace CX.Engine.Common.Stores.Graphs;

public class JsonEdgeStore : IDisposable
{
    public readonly string Name;
    public readonly JsonSerializerSettings JsonSerializerSettings = new()
    {
        TypeNameHandling = TypeNameHandling.All,
        DefaultValueHandling = DefaultValueHandling.Include,
        NullValueHandling = NullValueHandling.Ignore
    };

    private readonly ILogger _logger;
    private readonly IServiceProvider _sp;
    private readonly IDisposable _optionsMonitorDisposable;
    private readonly SemaphoreSlim _setSnapshotSlimLock = new(1, 1);
    private readonly TaskCompletionSource _tcsInitCompleted = new();
    private Snapshot _snapshot;

    private class Snapshot
    {
        public JsonEdgeStoreOptions Options;
        public PostgreSQLClient Sql;
    }

    private async void SetSnapshot(JsonEdgeStoreOptions options)
    {
        using var _ = await _setSnapshotSlimLock.UseAsync(); 
        
        try
        {
            var ss = new Snapshot();
            ss.Options = options;
            ss.Sql = _sp.GetRequiredNamedService<PostgreSQLClient>(options.PostgreSQLClientName);
            await InitAsync(ss);
            _snapshot = ss;
            _tcsInitCompleted.TrySetResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set snapshot.  New configuration will be ignored.");
        }
    }
    
    private async Task InitAsync(Snapshot ss)
    {
        await ss.Sql.ExecuteAsync(
            $"""
             CREATE TABLE IF NOT EXISTS {new InjectRaw(ss.Options.TableName)} (
                 id BIGSERIAL PRIMARY KEY,
                 source VARCHAR({new InjectRaw(ss.Options.MaxKeyLength.ToString())}) NOT NULL,
                 target VARCHAR({new InjectRaw(ss.Options.MaxKeyLength.ToString())}) NOT NULL,
                 meta JSONB
             );
             """);
    }

    public async Task ClearAsync()
    {
        await _tcsInitCompleted.Task;
        
        var ss = _snapshot;
        await ss.Sql.ExecuteAsync($"TRUNCATE TABLE {new InjectRaw(ss.Options.TableName)}");
    }
    
    public async Task AddAsync(JsonEdge jsonEdge)
    {
        await _tcsInitCompleted.Task;
        
        var ss = _snapshot;
        //return the id of the inserted row
        jsonEdge.Id = await ss.Sql.ScalarAsync<long>(
            $"""
             INSERT INTO {new InjectRaw(ss.Options.TableName)} (source, target, meta)
             VALUES ({jsonEdge.Source}, {jsonEdge.Target}, {JsonConvert.SerializeObject(jsonEdge.Meta)}::jsonb)
             RETURNING id;
             """);
    }

    public async Task AddManyAsync(IEnumerable<JsonEdge> edges)
    {
        await _tcsInitCompleted.Task;
        
        var ss = _snapshot;
        
        var sources = edges.Select(e => e.Source).ToArray();
        var targets = edges.Select(e => e.Target).ToArray();
        var metas = edges.Select(e => JsonConvert.SerializeObject(e.Meta)).ToArray();
        
        //use unnest
        await ss.Sql.ExecuteAsync(
            $"""
             INSERT INTO {new InjectRaw(ss.Options.TableName)} (source, target, meta)
             SELECT * FROM UNNEST({sources}, {targets}, {metas}::jsonb[]) AS t(source, target, meta);
             """);
    }

    public async Task RemoveAsync(long id)
    {
        await _tcsInitCompleted.Task;
        
        var ss = _snapshot;
        await ss.Sql.ExecuteAsync(
            $"""
             DELETE FROM {new InjectRaw(ss.Options.TableName)}
             WHERE id = {id};
             """);
    }
    
    public async Task RemoveAsync(JsonEdge jsonEdge)
    {
        if (!jsonEdge.Id.HasValue)
            throw new InvalidOperationException("Edge must have an id to remove.");
        
        await RemoveAsync(jsonEdge.Id.Value);
    }

    public async Task<List<JsonEdge>> GetEdgesForKeyAsync(string key)
    {
        await _tcsInitCompleted.Task;
        
        var ss = _snapshot;
        return await ss.Sql.ListAsync<JsonEdge>(
            $"""
             SELECT * FROM {new InjectRaw(ss.Options.TableName)}
             WHERE source = {key} OR target = {key}
             """, JsonEdge.Map);
    }

    public async Task RemoveAllWithEdgeTypeAsync(string edgeType)
    {
        await _tcsInitCompleted.Task;
        
        var ss = _snapshot;
        await ss.Sql.ExecuteAsync(
            $"""
             DELETE FROM {new InjectRaw(ss.Options.TableName)}
             WHERE meta->>'type' = {edgeType}
             """);
    }

    public async Task UpdateAsync(JsonEdge jsonEdge)
    {
        if (!jsonEdge.Id.HasValue)
            throw new InvalidOperationException("Edge must have an id to update.");
        
        await _tcsInitCompleted.Task;
        
        var ss = _snapshot;
        await ss.Sql.ExecuteAsync(
            $"""
             UPDATE {new InjectRaw(ss.Options.TableName)}
             SET source = {jsonEdge.Source}, target = {jsonEdge.Target}, meta = {JsonConvert.SerializeObject(jsonEdge.Meta, JsonSerializerSettings)}::jsonb
             WHERE id = {jsonEdge.Id}
             """);
    }

    public JsonEdgeStore([NotNull] string name, IOptionsMonitor<JsonEdgeStoreOptions> monitor, [NotNull] ILogger logger, [NotNull] IServiceProvider sp)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _sp = sp ?? throw new ArgumentNullException(nameof(sp));
        _optionsMonitorDisposable = monitor.Snapshot(() => _snapshot?.Options, SetSnapshot, logger, sp);
    }

    public void Dispose()
    {
        _optionsMonitorDisposable?.Dispose();
    }
}
using CX.Engine.Common.PostgreSQL;
using CX.Engine.Common.Stores.Graphs;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Npgsql;

namespace CX.Engine.Common.Stores.Json;

public class JsonObjectStore : IDisposable
{
    public readonly string Name;
    public readonly JsonSerializerSettings JsonSerializerSettings = new()
    {
        TypeNameHandling = TypeNameHandling.All,
        DefaultValueHandling = DefaultValueHandling.Include,
        NullValueHandling = NullValueHandling.Ignore
    };

    public KeyedSemaphoreSlim MutateLock { get; } = new();
    private readonly ILogger _logger;
    private readonly IDisposable _optionsMonitorDisposable;
    private readonly SemaphoreSlim _setSnapshotSlimLock = new(1, 1);
    private readonly TaskCompletionSource _tcsInitCompleted = new();
    private readonly IServiceProvider _sp;
    private Snapshot _snapshot;
    
    private class Snapshot
    {
        public JsonObjectStoreOptions Options;
        public PostgreSQLClient Sql;
    }

    private async void SetSnapshot(JsonObjectStoreOptions options)
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
        await ss.Sql.ExecuteAsync($"CREATE TABLE IF NOT EXISTS {new InjectRaw(ss.Options.TableName)} (key varchar({new InjectRaw(ss.Options.MaxKeyLength.ToString())}) PRIMARY KEY, value JSONB);");
    }

    public async Task ClearAsync()
    {
        await _tcsInitCompleted.Task;
        
        var ss = _snapshot;
        await ss.Sql.ExecuteAsync($"TRUNCATE TABLE {new InjectRaw(ss.Options.TableName)};");
    }
    
    public Task AddAsync<T>(T obj) where T : IStoreObject => AddAsync(obj.StoreKey, obj);

    public async Task AddAsync<T>(string key, T obj)
    {
        await _tcsInitCompleted.Task;

        var ss = _snapshot;
        //Use newtonsoft to serialize the object
        var json = JsonConvert.SerializeObject(obj, JsonSerializerSettings);
        await ss.Sql.ExecuteAsync($"INSERT INTO {new InjectRaw(ss.Options.TableName)} (key, value) VALUES ({key}, {json}::jsonb);");
    }

    public async Task<bool> KeyExistsAsync(string key)
    {
        await _tcsInitCompleted.Task;
        
        var ss = _snapshot;
        var res = await ss.Sql.ScalarAsync<int?>($"SELECT 1 FROM {new InjectRaw(ss.Options.TableName)} WHERE key = {key}");
        if (res != 1)
            return false;

        return true;
    }

    public Task<object> GetAsync(string key) => GetAsync<object>(key);
    public async Task<object> GetAsync(string key, object defaultValue) => await GetAsync<object>(key) ?? defaultValue;

    public async Task<T> GetAsync<T>(string key)
    {
        await _tcsInitCompleted.Task;

        var ss = _snapshot;
        var json = await ss.Sql.ScalarAsync<string>($"SELECT value FROM {new InjectRaw(ss.Options.TableName)} WHERE key = {key}");
        if (json == null)
            return default;
        
        var type = JToken.Parse(json)["$type"]?.Value<string>();
        if (type != null)
        {
            //find the type
            var typeToDeserialize = Type.GetType(type);
            if (typeToDeserialize == null)
                throw new InvalidOperationException($"Type {type} not found.");

            var obj = JsonConvert.DeserializeObject(json, typeToDeserialize, JsonSerializerSettings);
            return (T)obj;
        }

        return JsonConvert.DeserializeObject<T>(json, JsonSerializerSettings);
    }

    public Task<List<T>> GetAllAsync<T>(NpgsqlTransaction trans) => GetAllAsync<T>(trans?.Connection);

    public async Task<List<T>> GetAllAsync<T>(NpgsqlConnection con = null)
    {
        await _tcsInitCompleted.Task;

        var ss = _snapshot;
        var jsons = await ss.Sql.ListStringAsync($"SELECT value FROM {new InjectRaw(ss.Options.TableName)} WHERE value->>'$type' = {typeof(T).FullName + ", " + typeof(T).Namespace}", con);

        if (jsons == null)
            return [];
        
        return jsons.Select(json => JsonConvert.DeserializeObject<T>(json, JsonSerializerSettings)).ToList();
    }

    public async Task<List<string>> GetAllKeysAsync<T>()
    {
        await _tcsInitCompleted.Task;

        var ss = _snapshot;
        var jsons = await ss.Sql.ListStringAsync($"SELECT key FROM {new InjectRaw(ss.Options.TableName)} WHERE value->>'$type' = {typeof(T).FullName + ", " + typeof(T).Namespace}");

        if (jsons == null)
            return [];

        return jsons;
    }

    public async Task DeleteAsync(string key)
    {
        await _tcsInitCompleted.Task;

        var ss = _snapshot;
        await ss.Sql.ExecuteAsync($"DELETE FROM {new InjectRaw(ss.Options.TableName)} WHERE key = {key}");
    }

    public Task SetAsync<T>(T obj, NpgsqlConnection con = null) where T: IStoreObject => SetAsync(obj.StoreKey, obj, con);
    public Task SetAsync<T>(T obj, NpgsqlTransaction trans) where T: IStoreObject => SetAsync(obj.StoreKey, obj, trans?.Connection);

    public async Task SetAsync<T>(string key, T obj, NpgsqlConnection con = null)
    {
        await _tcsInitCompleted.Task;

        var ss = _snapshot;
        var json = JsonConvert.SerializeObject(obj, JsonSerializerSettings);
        
        await ss.Sql.ExecuteAsync($"INSERT INTO {new InjectRaw(ss.Options.TableName)} (key, value) VALUES ({key}, {json}::jsonb) ON CONFLICT (key) DO UPDATE SET value = {json}::jsonb", con);
    }

    public async Task SetManyAsync<T>(IEnumerable<T> objs) where T : IStoreObject
    {
        await _tcsInitCompleted.Task;

        var keys = new List<string>();
        var vals = new List<T>();
        foreach (var obj in objs)
        {
            keys.Add(obj.StoreKey);
            vals.Add(obj);
        }

        await SetManyAsync(keys.ToArray(), vals.ToArray());
    }

    public async Task SetManyAsync<T>(string[] keys, params T[] objs)
    {
        await _tcsInitCompleted.Task;
        
        if (keys.Length != objs.Length)
            throw new ArgumentException("Keys and objects must be the same length.");

        const int batchSize = 10_000;
        if (keys.Length > batchSize)
        {
            //split into batches
            for (var i = 0; i < keys.Length; i += batchSize)
            {
                var keysBatch = keys.Skip(i).Take(batchSize).ToArray();
                var objsBatch = objs.Skip(i).Take(batchSize).ToArray();
                await SetManyAsync(keysBatch, objsBatch);
            }

            return;
        }


        var ss = _snapshot;
        var jsons = objs.Select(obj => JsonConvert.SerializeObject(obj, JsonSerializerSettings)).ToArray();
        await ss.Sql.ExecuteAsync($"INSERT INTO {new InjectRaw(ss.Options.TableName)} (key, value) SELECT * FROM UNNEST({keys}, {jsons}::jsonb[]) AS t(key, value) ON CONFLICT (key) DO UPDATE SET value = EXCLUDED.value::jsonb");
    }

    public async Task RemoveAllAsync<T>()
    {
        await _tcsInitCompleted.Task;

        var ss = _snapshot;
        await ss.Sql.ExecuteAsync($"DELETE FROM {new InjectRaw(ss.Options.TableName)} WHERE value->>'$type' = {typeof(T).FullName + ", " + typeof(T).Namespace}");
    }

    public Task MutateAsync<T>(T obj, Action<T> action) where T : IStoreObject, new() => MutateAsync(obj.StoreKey, action); 

    public async Task MutateAsync<T>(string key, Action<T> action) where T: new()
    {
        using var _ = await MutateLock.UseAsync(key);
        var obj = await GetAsync<T>(key) ?? new();
        action(obj);
        await SetAsync(key, obj);
    }

    public async Task<ObjectEdge> ResolveEdgeAsync(JsonEdge edge)
    {
        var sourceObj = await GetAsync(edge.Source, edge.Source);
        var destObj = await GetAsync(edge.Target, edge.Target);
        return new ObjectEdge
        {
            Id = edge.Id,
            Source = sourceObj,
            Target = destObj,
            Meta = edge.Meta
        };
    }

    public async Task<List<ObjectEdge>> ResolveEdgesAsync(List<JsonEdge> edges, Dictionary<string, Task<object>> objCache = null)
    {
        objCache ??= new();
        var res = new List<ObjectEdge>();

        async Task LocalResolveEdgeAsync(JsonEdge edge)
        {
            var outEdge = new ObjectEdge();
            outEdge.Id = edge.Id;
            outEdge.Meta = edge.Meta;

            if (!objCache.TryGetValue(edge.Source, out var sourceTask))
                objCache[edge.Source] = sourceTask = GetAsync(edge.Source, edge.Source);
            if (!objCache.TryGetValue(edge.Target, out var targetTask))
                objCache[edge.Target] = targetTask = GetAsync(edge.Target, edge.Target);

            outEdge.Source = await sourceTask;
            outEdge.Target = await targetTask;

            lock (res)
                res.Add(outEdge);
        }

        await (from edge in edges select LocalResolveEdgeAsync(edge));

        return res;
    }

    public JsonObjectStore([NotNull] string name, IOptionsMonitor<JsonObjectStoreOptions> monitor, [NotNull] ILogger logger, IServiceProvider sp)
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
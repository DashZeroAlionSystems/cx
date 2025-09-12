using System.Collections.Concurrent;
using System.Data.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CX.Engine.Common.PostgreSQL;

public class QueryCache : IDisposable
{
    private bool _isDisposed;
    private QueryCacheOptions _options;
    private readonly IDisposable _optionsSnapshotDisposable;
    public readonly ILogger<QueryCache> Logger;
    
    public ConcurrentDictionary<string, QueryCacheEntry> Queries { get; } = new();


    public QueryCache(ILogger<QueryCache> logger, IOptionsMonitor<QueryCacheOptions> options, IServiceProvider sp)
    {
        _options = options.CurrentValue;
        _optionsSnapshotDisposable = options.Snapshot(() => _options, v => _options = v, logger, sp);
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        StartExpiryLoop();
    }

    public class QueryCacheEntry
    {
        public Task<object> CachedResult;
        public Task<object> NewCachedResult;
        public DateTime LastExecuted;
        public DateTime LastUsed;
    }

    private async void StartExpiryLoop()
    {
        while (_isDisposed)
        {
            await Task.Delay(100);
            var expired = Queries.Where(x => (DateTime.UtcNow - x.Value.LastUsed) > _options.CacheEntryExpiresAfter).ToArray();
            foreach (var ex in expired)
                Queries.TryRemove(ex.Key, out _);
        }
    }

    public async Task<List<TValue>> ListAsync<TValue>(PostgreSQLClient client, string query, Func<DbDataReader, TValue> map)
    {
        var entry = Queries.GetOrAdd(query, new QueryCacheEntry
        {
            CachedResult = null,
            LastUsed = DateTime.UtcNow,
            LastExecuted = DateTime.UtcNow
        });

        async Task<object> RunQuery()
        {
            return await client.ListAsync(query, map);
        }

        entry.LastUsed = DateTime.UtcNow;

        if (entry.NewCachedResult?.IsCompleted ?? false)
        {
            try
            {
                await entry.NewCachedResult;
            }
            catch (Exception ex)
            {
                lock (entry)
                    entry.NewCachedResult = null;
                Logger.LogError(ex, "During swapping in new cached query results");
            }

            lock (entry)
            {
                entry.CachedResult = entry.NewCachedResult;
                entry.NewCachedResult = null;
            }
        }

        var ss = _options;
        if ((DateTime.UtcNow - entry.LastExecuted) > ss.StartCacheEntryRefreshAfter)
        {
            lock (entry)
            {
                if ((DateTime.UtcNow - entry.LastExecuted) > ss.StartCacheEntryRefreshAfter && entry.NewCachedResult == null)
                {
                    entry.LastExecuted = DateTime.UtcNow;
                    entry.NewCachedResult = RunQuery();
                }
            }
        }

        if (entry.CachedResult == null)
            lock (entry)
            {
                if (entry.CachedResult == null)
                {
                    entry.CachedResult = RunQuery();
                    Queries[query] = entry;
                }
            }

        try
        {
            return (List<TValue>)await entry.CachedResult;
        }
        catch 
        {
            Queries.TryRemove(query, out _);
            throw;
        }
    }

    public void Dispose()
    {
        _optionsSnapshotDisposable?.Dispose();
        _isDisposed = true;
    }
}
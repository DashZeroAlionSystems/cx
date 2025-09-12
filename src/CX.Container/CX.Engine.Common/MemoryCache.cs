using System.Collections.Concurrent;
using JetBrains.Annotations;

namespace CX.Engine.Common;

[PublicAPI]
public class MemoryCache<TKey, TValue> : IDisposable 
{
    public class CacheEntry
    {
        public TValue Value;
        public DateTime LastAccessedUtc;
        public DateTime CreationTimeUTC;

        public CacheEntry(TValue value)
        {
            Value = value;
            CreationTimeUTC = DateTime.UtcNow;
        }
    }

    private readonly ConcurrentDictionary<TKey, CacheEntry> _cache;
    private MemoryCacheOptions _options;
    private readonly CancellationTokenSource _ctsDisposed = new();
    private CancellationTokenSource _ctsNewConfig = new();

    public MemoryCache(MemoryCacheOptions options, IEqualityComparer<TKey> comparer = null)
    {
        _cache = new(comparer ?? EqualityComparer<TKey>.Default);
        _options = options ?? throw new ArgumentNullException(nameof(options));
        options.Validate();
        
        if (_options.ExpiryCheckInterval.HasValue)
            _ = Task.Run(async () =>
            {
                while (!_ctsDisposed.IsCancellationRequested)
                {
                    if (_ctsNewConfig.IsCancellationRequested)
                        _ctsNewConfig = new();
                    
                    try
                    {
                        await Task.Delay(_options.ExpiryCheckInterval.Value, _ctsNewConfig.Token);
                    }
                    catch (TaskCanceledException)
                    {
                        continue;
                    }

                    CheckExpiry();
                }
            });
    }

    public void UpdateMemoryCacheOptions(MemoryCacheOptions options)
    {
        _options = options;
        _ctsNewConfig.Cancel();
    }

    public void CheckExpiry()
    {
        if (_options.EntriesExpiresAfterNoAccessDuration == null && _options.EntriesExpiresAfterCreation == null)
            return;

        var now = DateTime.UtcNow;
        foreach (var (key, entry) in _cache)
        {
            var UTCCheckTime = _options.EntriesExpiresAfterCreation != null ? entry.CreationTimeUTC : entry.LastAccessedUtc;
            var ExpireTimeSpan = _options.EntriesExpiresAfterCreation ?? _options.EntriesExpiresAfterNoAccessDuration;
            if (now - UTCCheckTime > ExpireTimeSpan)
                _cache.TryRemove(key, out _);
        }
    }

    public TValue GetOrAdd(TKey key, Func<TKey, TValue> value)
    {
        var entry = _cache.GetOrAdd(key, _ => new(value(key)));
        entry.LastAccessedUtc = DateTime.UtcNow;
        return entry.Value;
    }

    public TValue Get(TKey key)
    {
        if (_cache.TryGetValue(key, out var res))
            return res.Value;

        return default;
    }

    public bool TryRemove(TKey key, out TValue value)
    {
        if (_cache.TryRemove(key, out var entry))
        {
            value = entry.Value;
            return true;
        }
        else
        {
            value = default;
            return false;
        }
    }
    
    public bool TryGetValue(TKey key, out TValue value)
    {
        if (_cache.TryGetValue(key, out var entry))
        {
            value = entry.Value;
            return true;
        }
        else
        {
            value = default;
            return false;
        }
    }

    public TValue this[TKey key]
    {
        get => _cache[key].Value;
        set
        {
            var entry = new CacheEntry(value);
            entry.LastAccessedUtc = DateTime.UtcNow;
            _cache[key] = entry;
        }
    }

    public void Dispose()
    {
        _ctsDisposed.Cancel();
    }
}
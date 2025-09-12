namespace CX.Engine.Common;

/// <summary>
/// Manages a collection of single-entry <see cref="SemaphoreSlim"/> with <see cref="string"/> unique keys.
/// </summary>
public class KeyedSemaphoreSlim
{
    private class KeyedLock
    {
        public readonly SemaphoreSlim SlimLock = new(1, 1);
        public int OpenCount;
    }

    private readonly Dictionary<string, KeyedLock> _locks = new(StringComparer.InvariantCultureIgnoreCase);

    /// <summary>
    /// Get a lock for the given key.
    /// </summary>
    /// <param name="key">The key to get a lock for.</param>
    /// <returns>A lock for the given key.</returns>
    private KeyedLock GetAndInc(string key)
    {
        lock (_locks)
        {
            if (_locks.TryGetValue(key, out var keyedLock))
            {
                keyedLock.OpenCount++;
                return keyedLock;
            }

            keyedLock = new();
            _locks[key] = keyedLock;
            keyedLock.OpenCount++;

            return keyedLock;
        }
    }

    /// <summary>
    /// Acquire a semaphore slim with the provided key.
    /// </summary>
    /// <param name="key">The key to acquire a semaphore for.</param>
    /// <exception cref="ArgumentNullException">Thrown if the key is null.</exception>
    public Task WaitAsync(string key)
    {
        if (key == null) 
            throw new ArgumentNullException(nameof(key));
        
        var keyedLock = GetAndInc(key);
        return keyedLock.SlimLock.WaitAsync();
    }
    
    public async Task<KeyedSemaphoreSlimDisposable> UseAsync(string key)
    {
        await WaitAsync(key);
        return new(this, key);
    }

    public async Task<MultiKeyedSemaphoreSlimDisposable> UseAsync(HashSet<string> keys)
    {
        if (keys == null)
            return new();

        if (keys.Count == 0)
            return new();

        // Optional: deduplicate keys to prevent locking the same key multiple times.
        // (This is recommended if your underlying locks are non-reentrant.)
        var distinctKeys = keys.Distinct().ToArray();

        // Sort the keys to impose a consistent locking order.
        Array.Sort(distinctKeys, StringComparer.Ordinal);

        // Acquire each lock in the sorted order.
        foreach (var key in distinctKeys)
            if (key != null)
                await WaitAsync(key);

        // Return a disposable that will release the locks when disposed.
        return new(this, distinctKeys);
    }
    
    /// <summary>
    /// Release the semaphore slim with the provided key.
    /// </summary>
    /// <param name="key">The key to release the semaphore for.</param>
    /// <exception cref="InvalidOperationException">Thrown if the lock does not exist.</exception>
    public void Release(string key)
    {
        lock (_locks)
        {
            if (_locks.TryGetValue(key, out var keyedLock))
            {
                keyedLock.OpenCount--;
                keyedLock.SlimLock.Release();

                if (keyedLock.OpenCount == 0)
                    _locks.Remove(key);
            }
            else
                throw new InvalidOperationException("Lock does not exist");
        }
    }

    public bool IsHeld(string key)
    {
        lock (_locks)
            return _locks.ContainsKey(key);
    }
}
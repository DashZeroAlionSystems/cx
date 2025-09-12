namespace CX.Engine.Common;

public readonly struct MultiKeyedSemaphoreSlimDisposable : IDisposable
{
    private readonly KeyedSemaphoreSlim _slimLock;
    private readonly string[] _keys;

    public MultiKeyedSemaphoreSlimDisposable()
    {
    }

    public MultiKeyedSemaphoreSlimDisposable(KeyedSemaphoreSlim slimLock, string[] keys)
    {
        _slimLock = slimLock ?? throw new ArgumentNullException(nameof(slimLock));
        _keys = keys;
    }

    /// <summary>
    /// NB: Only call once.  
    /// </summary>
    public void Dispose()
    {
        if (_keys != null)
            foreach (var key in _keys)
                if (key != null)
                    _slimLock.Release(key);
    }
}
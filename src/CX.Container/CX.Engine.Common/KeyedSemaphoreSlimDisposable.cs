namespace CX.Engine.Common;

public readonly struct KeyedSemaphoreSlimDisposable : IDisposable
{
    private readonly KeyedSemaphoreSlim _slimLock;
    private readonly string _key;

    public KeyedSemaphoreSlimDisposable(KeyedSemaphoreSlim slimLock, string key)
    {
        _slimLock = slimLock ?? throw new ArgumentNullException(nameof(slimLock));
        _key = key ?? throw new ArgumentNullException(nameof(key));
    }

    /// <summary>
    /// NB: Only call once.  
    /// </summary>
    public void Dispose()
    {
        _slimLock.Release(_key);
    }
}
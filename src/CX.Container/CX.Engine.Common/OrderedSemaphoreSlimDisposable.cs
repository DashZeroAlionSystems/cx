namespace CX.Engine.Common;

public struct OrderedSemaphoreSlimDisposable : IDisposable
{
    private OrderedSemaphoreSlim _slimLock;

    public OrderedSemaphoreSlimDisposable(OrderedSemaphoreSlim slimLock)
    {
        _slimLock = slimLock ?? throw new ArgumentNullException(nameof(slimLock));
    }
    
    public void Release()
    {
        _slimLock?.Release();
        _slimLock = null;
    }

    public void Dispose() => Release();
}
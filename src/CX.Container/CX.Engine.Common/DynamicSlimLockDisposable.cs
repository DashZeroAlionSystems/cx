namespace CX.Engine.Common;

public struct DynamicSlimLockDisposable : IDisposable
{
    private DynamicSlimLock _slimLock;

    public DynamicSlimLockDisposable(DynamicSlimLock slimLock)
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
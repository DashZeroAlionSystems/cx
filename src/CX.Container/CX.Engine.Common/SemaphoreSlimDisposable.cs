namespace CX.Engine.Common;

public struct SemaphoreSlimDisposable : IDisposable
{
    private SemaphoreSlim _slimLock;
    
    public SemaphoreSlimDisposable(SemaphoreSlim slimLock)
    {
        _slimLock = slimLock;
    }
    
    public void Release()
    {
        _slimLock?.Release();
        _slimLock = null;
    }

    public void Dispose() => Release();
}
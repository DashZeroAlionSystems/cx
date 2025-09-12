namespace CX.Engine.Common;

public interface IDisposeTracker
{
    public void TrackDisposable(IDisposable disposable);
}
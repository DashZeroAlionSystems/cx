namespace CX.Engine.Common;

public class DisposeTracker : IDisposeTracker, IDisposable
{
    public readonly List<IDisposable> TrackedDisposables = new();
    
    public virtual void Dispose()
    {
        foreach (var d in TrackedDisposables)
            d.Dispose();
    }

    public void TrackDisposable(IDisposable disposable)
    {
        TrackedDisposables.Add(disposable);
    }
}
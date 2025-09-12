using System.Diagnostics.Metrics;

namespace CX.Container.Server.Metrics;

public class TrackedDuration : IDisposable
{
    private readonly long _startTime = TimeProvider.System.GetTimestamp();
    private readonly Histogram<double> _histogram;

    public TrackedDuration(Histogram<double> histogram)
    {
        _histogram = histogram;
    }
    
    public void Dispose()
    {
        var elapsed = TimeProvider.System.GetElapsedTime(_startTime);
        _histogram.Record(elapsed.TotalMilliseconds);
    }
}
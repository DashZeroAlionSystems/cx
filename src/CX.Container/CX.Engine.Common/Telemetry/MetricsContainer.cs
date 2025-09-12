using Microsoft.Extensions.DependencyInjection;

namespace CX.Engine.Common.Telemetry;

public abstract class MetricsContainer : IMetricsContainer, IDisposable
{
    private readonly ITelemetryRecorder[] _recorders;
    
    protected MetricsContainer(IServiceProvider sp, string type, string instance)
    {
        Type = type;
        Instance = instance;
        _recorders = sp.GetServices<ITelemetryRecorder>().ToArray();
        foreach (var recorder in _recorders)
            recorder.CollectMetricsFrom.Add(this);
    }

    public string Type { get; }
    public string Instance { get; }
    public Guid InstanceId { get; } = Guid.NewGuid();
    public abstract string ToJson();

    public void Dispose()
    {
        foreach (var recorder in _recorders)
            recorder.CollectMetricsFrom.Remove(this);
    }
}
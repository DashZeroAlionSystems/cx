namespace CX.Engine.Common.Telemetry;

public interface ITelemetryRecorder
{
    HashSet<IMetricsContainer> CollectMetricsFrom { get; }
}
namespace CX.Engine.Common.Telemetry;

public interface IMetricsContainer
{
    string Type { get; }
    string Instance { get; }
    Guid InstanceId { get; }

    string ToJson();
}
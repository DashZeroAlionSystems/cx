namespace CX.Engine.Common.Telemetry;

public interface IMetric<T>
{
    void Observe(T value);
}
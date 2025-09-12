namespace CX.Engine.Common.Telemetry;

public sealed class IncrementalLifetimeIntMetric : IMetric<int>
{
    private int _value;
    public int Value => _value;

    public void Observe(int value)
    {
        Interlocked.Add(ref _value, value);
    }

    public void Inc(int value = 1) => Observe(value);
}
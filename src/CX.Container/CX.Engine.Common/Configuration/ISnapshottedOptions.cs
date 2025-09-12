namespace CX.Engine.Common;

public interface ISnapshottedOptions<TSnapshot, TOptions, TInstance> : IDisposable
    where TOptions: class
    where TSnapshot : Snapshot<TOptions, TInstance>
{
    public TSnapshot CurrentShapshot { get; set; }
    public MonitoredOptionsSection<TOptions> OptionsSection { get; set; }
    
    void IDisposable.Dispose()
    {
        OptionsSection?.Dispose();
    }
}
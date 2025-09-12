using JetBrains.Annotations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CX.Engine.Common;

public class MonitoredOptionsSection<TOptions> : IDisposable 
    where TOptions: class 
{
    private IDisposable _optionsMonitorDisposable;

    public readonly IConfigurationSection Section;
    public readonly IOptionsMonitor<TOptions> Monitor;
    public readonly ILogger Logger;
    public readonly IServiceProvider Sp;
    
    public MonitoredOptionsSection([NotNull] IConfigurationSection section, [NotNull] IOptionsMonitor<TOptions> monitor, ILogger logger, IServiceProvider sp)
    {
        Section = section ?? throw new ArgumentNullException(nameof(section));
        Monitor = monitor ?? throw new ArgumentNullException(nameof(monitor));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        Sp = sp ?? throw new ArgumentNullException(nameof(sp));
    }

    public void Bind<TSnapshot, TInstance>(TInstance parent)
        where TSnapshot: Snapshot<TOptions, TInstance>, new() where TInstance: ISnapshottedOptions<TSnapshot, TOptions, TInstance>
    {
        if (_optionsMonitorDisposable != null)
        {
            _optionsMonitorDisposable?.Dispose();
            _optionsMonitorDisposable = null;
        }

        _optionsMonitorDisposable = Monitor.Snapshot(() => parent.CurrentShapshot?.Options, opts =>
        {
            var ss = new TSnapshot();
            ss.Instance = parent;
            ss.Options = opts;
            if (ss is ISnapshotSyncInit<TOptions> sync)
                sync.Init(Section, Logger, Sp);
            parent.CurrentShapshot = ss;
        }, Logger, Sp, Section);
    }

    public void Dispose()
    {
        _optionsMonitorDisposable?.Dispose();
    }
}
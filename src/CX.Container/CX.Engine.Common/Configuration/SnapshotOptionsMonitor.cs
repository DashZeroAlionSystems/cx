using System.Reflection;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CX.Engine.Common;

public class ValidatedOptionsMonitor : IDisposable
{
    public IDisposable OnChangeMonitorDisposable;
    public ILogger Logger;
    public Action<bool> Exists;
    public IConfigurationSection MonitoredSection;
    public readonly OrderedSemaphoreSlim ChangeQueue = new(1);

    public void Dispose()
    {
        OnChangeMonitorDisposable?.Dispose();
    }
}

public sealed class SnapshotOptionsMonitor<T> : ValidatedOptionsMonitor
{
    public IOptionsMonitor<T> Monitor;
    public Func<T> Get;
    public Action<T> Set;
    public Action<T> InitialSet;
    public IServiceProvider ServiceProvider;
    private IValidatorFor<T> _validator;
    private bool _checkedValidationType;

    public void Validate(T snapshot)
    {
        if (snapshot is IValidatable ivalidate)
            ivalidate.Validate();
        
        if (snapshot is IValidatableConfiguration ivalidateConfig)
            ivalidateConfig.Validate(MonitoredSection);

        if (!_checkedValidationType)
            SetupValidation();
        
        _validator?.Validate(snapshot);
    }

    private void SetupValidation()
    {
        try
        {
            var attr = typeof(T).GetCustomAttribute<ValidatedByAttribute>();
            if (attr == null)
                return;

            if (attr.ValidationType == null)
                return;
            
            var validator = ServiceProvider.GetService(attr.ValidationType) as IValidatorFor<T>;

            if (validator == null)
                return;

            _validator = validator;
        }
        finally
        {
            _checkedValidationType = true;
        }
    }

    public void Start()
    {
        var snapshot = Monitor.CurrentValue;

        Validate(snapshot);

        if (MonitoredSection != null)
            Exists?.Invoke(MonitoredSection.Exists());
        InitialSet(snapshot);

        OnChangeMonitorDisposable = Monitor.OnChange(Listener);
    }

    public async void NotifyChange(T newOpts = default)
    {
        try
        {
            using var _ = await ChangeQueue.UseAsync();
            Set(newOpts ?? Monitor.CurrentValue);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error setting new options.");
        }
    }

    private void Listener(T newOpts)
    {
        if (MonitoredSection != null)
        {
            var sectionExists = MonitoredSection.Exists();
            Exists?.Invoke(sectionExists);

            if (!sectionExists)
                return;
        }

        try
        {
            Validate(newOpts);
            if (JsonSerializer.Serialize(Get()) == JsonSerializer.Serialize(newOpts)) return;

            Logger.LogInformation("New options received and activated.");
            NotifyChange(newOpts);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error validating new options:  They will be ignored.");
        }
    }
}
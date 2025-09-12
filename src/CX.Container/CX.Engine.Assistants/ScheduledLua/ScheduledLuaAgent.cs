using CX.Engine.Assistants.ScheduledQuestions;
using CX.Engine.Common;
using CX.Engine.Common.DistributedLocks;
using JetBrains.Annotations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CX.Engine.Assistants.ScheduledLua;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class ScheduledLuaAgent : IDisposable
{
    private readonly IServiceProvider _sp;
    private readonly ILogger _logger;
    private CancellationTokenSource _cts = new();
    private readonly IDisposable _optionsChangeDisposable;
    private bool _optionsExists;
    private readonly DistributedLockService _distributedLockService;
    private readonly OrderedSemaphoreSlim _slimLockSetOptions = new(1);
    private readonly string _name;
    private Snapshot _snapshot;

    // Add a private field to hold the Task reference
    private Task _runningTask;

    private readonly object _ctsOptionsChangedLock = new();

    public class Snapshot
    {
        public ScheduledLuaAgentOptions Options;
        public readonly CancellationTokenSource NewSnapshotIssued = new();
        public LuaInstance LuaInstance;
    }

    private async void SetOptions(ScheduledLuaAgentOptions opts)
    {
        try
        {
            using var _ = await _slimLockSetOptions.UseAsync();
            var ss = new Snapshot();
            ss.Options = opts;
            ss.LuaInstance = _sp.GetRequiredNamedService<LuaCore>(opts.LuaCore).GetLuaInstance();
            ss.LuaInstance.Logger = null;
            _logger.LogTrace("Executing Setup LUA script");
            if (!string.IsNullOrWhiteSpace(ss.Options.SetupLua))
            {
                var res = await ss.LuaInstance.RunAsync(ss.Options.SetupLua);
                _logger.LogTrace("< " + res);
            }
            
            _snapshot?.NewSnapshotIssued.Cancel();
            _snapshot = ss;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Could not set new options");
        }
    }

    public ScheduledLuaAgent(
        IServiceProvider sp,
        ILogger logger,
        IConfigurationSection section,
        IOptionsMonitor<ScheduledLuaAgentOptions> options,
        DistributedLockService distributedLockService,
        string name
    )
    {
        _sp = sp;
        _logger = logger;
        _name = name ?? throw new ArgumentNullException(nameof(name));
        _optionsChangeDisposable = options.Snapshot(section, () => _snapshot?.Options, SetOptions, v => _optionsExists = v, _logger, sp);
        _distributedLockService =
            distributedLockService ?? throw new ArgumentNullException(nameof(distributedLockService));
    }

    public void Dispose()
    {
        _cts.Cancel();
        _optionsChangeDisposable.Dispose();
    }

    private bool CheckHold(Snapshot ss)
    {
        var utcNow = DateTime.UtcNow;

        // Map current day to RunningDays enum
        var todayRunningDay = DayOfWeekToRunningDay(utcNow.DayOfWeek);

        // Check if today is a running day
        var isTodayRunningDay = (ss.Options.RunDays & todayRunningDay) != RunningDays.None;

        // Check if current time is within allowed hours
        var isWithinHours = utcNow.Hour >= ss.Options.UTCStartHour && utcNow.Hour < ss.Options.UTCEndHour;

        // Hold if it's not a running day or outside allowed hours
        return !isTodayRunningDay || !isWithinHours;
    }

    private RunningDays DayOfWeekToRunningDay(DayOfWeek dayOfWeek)
    {
        return dayOfWeek switch
        {
            DayOfWeek.Sunday => RunningDays.Sunday,
            DayOfWeek.Monday => RunningDays.Monday,
            DayOfWeek.Tuesday => RunningDays.Tuesday,
            DayOfWeek.Wednesday => RunningDays.Wednesday,
            DayOfWeek.Thursday => RunningDays.Thursday,
            DayOfWeek.Friday => RunningDays.Friday,
            DayOfWeek.Saturday => RunningDays.Saturday,
            _ => throw new NotSupportedException(),
        };
    }
    
    private DateTime GetNextRunTime(Snapshot ss, DateTime fromTime)
    {
        var potentialTime = fromTime;

        while (true)
        {
            var dayOfWeek = potentialTime.DayOfWeek;
            var runningDay = DayOfWeekToRunningDay(dayOfWeek);

            var isRunningDay = (ss.Options.RunDays & runningDay) != RunningDays.None;

            if (isRunningDay)
            {
                // Check if current time is before start hour
                if (potentialTime.Hour < ss.Options.UTCStartHour)
                {
                    // Set to start hour
                    potentialTime = new(potentialTime.Year, potentialTime.Month, potentialTime.Day, ss.Options.UTCStartHour, 0, 0);
                    return potentialTime;
                }
                else if (potentialTime.Hour >= ss.Options.UTCStartHour && potentialTime.Hour < ss.Options.UTCEndHour)
                {
                    // Within allowed hours
                    return potentialTime;
                }
                else
                {
                    // After end hour, move to next day
                    potentialTime = potentialTime.AddDays(1).Date;
                }
            }
            else
            {
                // Not a running day, move to next day
                potentialTime = potentialTime.AddDays(1).Date;
            }
        }
    }

    public void Start()
    {
        if (_runningTask is { IsCompleted: true })
            return;
        
        if (_cts.IsCancellationRequested)
            _cts = new();

        _runningTask = Task.Run(async () =>
        {
            await using var _ = await _distributedLockService.UseAsync(_name, _cts.Token);
        
            while (!_cts.IsCancellationRequested)
            {
                var ss = _snapshot;
                try
                {
                    if (!_optionsExists)
                    {
                        await Task.Delay(1_000);
                        continue;
                    }

                    {
                        if (CheckHold(ss))
                        {
                            var utcNow = DateTime.UtcNow;

                            var nextRunTime = GetNextRunTime(ss, utcNow);

                            var holdTime = nextRunTime - utcNow;

                            _logger.LogTrace("Holding for {HoldTime}", holdTime.ToString(@"d\.hh\:mm\:ss"));
                            await Task.Delay(holdTime, ss.NewSnapshotIssued.Token);
                        }
                        
                        _logger.LogTrace("Executing LUA script");
                        var res = await ss.LuaInstance.RunAsync(ss.Options.RunLua);
                        _logger.LogTrace("< " + res);
                        await Task.Delay(ss.Options.IntervalPerRun, ss.NewSnapshotIssued.Token);
                    }
                }
                catch (TaskCanceledException tcx)
                {
                    _logger.LogTrace(tcx, "Task cancelled.");
                    if (tcx.CancellationToken == _cts.Token)
                        break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error in {nameof(ScheduledLuaAgent)}: {ex.Message}");
                    await Task.Delay(1_000);
                }
            }
        });
    }

    public async Task StopAsync()
    {
        await _cts.CancelAsync();
    }

    public bool IsRunning()
    {
        return _runningTask is { IsCompleted: false, IsCanceled: false };
    }
}
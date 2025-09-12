using CX.Engine.Common;
using CX.Engine.Common.DistributedLocks;
using CX.Engine.Common.PostgreSQL;
using JetBrains.Annotations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CX.Engine.Assistants.ScheduledQuestions;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class ScheduledQuestionAgent : IDisposable
{
    private readonly IServiceProvider _sp;
    private readonly ILogger _logger;
    private ScheduledQuestionAgentOptions _options;
    private IAssistant _assistant;
    private TimeSpan _interval; // Default
    private CancellationTokenSource _cts = new();
    private CancellationTokenSource _ctsOptionsChanged;
    private PostgreSQLClient _client;
    private readonly IDisposable _optionsChangeDisposable;
    private bool _optionsExists;
    private readonly DistributedLockService _distributedLockService;
    private readonly string _name;
    private IAsyncDisposable _lock;

    // Add a private field to hold the Task reference
    private Task _runningTask;

    private readonly object _ctsOptionsChangedLock = new();

    private void CancelAndRecreateCancellationToken()
    {
        lock (_ctsOptionsChangedLock)
        {
            if (_ctsOptionsChanged == null)
            {
                _ctsOptionsChanged = new();
                return;
            }

            if (!_ctsOptionsChanged.IsCancellationRequested)
            {
                _ctsOptionsChanged.Cancel();
            }

            _ctsOptionsChanged.Dispose();
            _ctsOptionsChanged = new();
        }
    }

    public ScheduledQuestionAgent(
        IServiceProvider sp,
        ILogger logger,
        IConfigurationSection section,
        IOptionsMonitor<ScheduledQuestionAgentOptions> options,
        DistributedLockService distributedLockService,
        string name
    )
    {
        _sp = sp;
        _logger = logger;
        _optionsChangeDisposable = options.Snapshot(section, () => _options, o =>
            {
                _options = o;
                if (_ctsOptionsChanged is { IsCancellationRequested: false })
                    CancelAndRecreateCancellationToken();
            },
            v => _optionsExists = v, _logger, sp);
        _distributedLockService =
            distributedLockService ?? throw new ArgumentNullException(nameof(distributedLockService));
        _name = name ?? throw new ArgumentNullException(nameof(name));
    }

    public void Dispose()
    {
        _cts.Cancel();
        _ctsOptionsChanged.Cancel();
        _optionsChangeDisposable.Dispose();
        _lock.DisposeAsync();
    }

    private bool CheckHold()
    {
        var utcNow = DateTime.UtcNow;

        // Map current day to RunningDays enum
        var todayRunningDay = DayOfWeekToRunningDay(utcNow.DayOfWeek);

        // Check if today is a running day
        bool isTodayRunningDay = (_options.RunDays & todayRunningDay) != RunningDays.None;

        // Check if current time is within allowed hours
        bool isWithinHours = utcNow.Hour >= _options.UTCStartHour && utcNow.Hour < _options.UTCEndHour;

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

    private DateTime GetNextRunTime(DateTime fromTime)
    {
        DateTime potentialTime = fromTime;

        while (true)
        {
            var dayOfWeek = potentialTime.DayOfWeek;
            var runningDay = DayOfWeekToRunningDay(dayOfWeek);

            bool isRunningDay = (_options.RunDays & runningDay) != RunningDays.None;

            if (isRunningDay)
            {
                // Check if current time is before start hour
                if (potentialTime.Hour < _options.UTCStartHour)
                {
                    // Set to start hour
                    potentialTime = new DateTime(potentialTime.Year, potentialTime.Month, potentialTime.Day, _options.UTCStartHour, 0, 0);
                    return potentialTime;
                }
                else if (potentialTime.Hour >= _options.UTCStartHour && potentialTime.Hour < _options.UTCEndHour)
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
        {
            return;
        }

        // Reset the cancellation token source
        if (_cts.IsCancellationRequested)
        {
            _cts = new();
        }

        _runningTask = Task.Run(async () =>
        {
            _lock = await _distributedLockService.UseAsync(_name, _cts.Token);

            while (!_cts.IsCancellationRequested)
            {
                try
                {
                    if (_ctsOptionsChanged?.IsCancellationRequested ?? true)
                    {
                        CancelAndRecreateCancellationToken();
                    }

                    if (!_optionsExists)
                    {
                        await Task.Delay(1_000);
                        continue;
                    }

                    _client = _sp.GetRequiredNamedService<PostgreSQLClient>(_options.PostgreSQLClientName);
                    _interval = TimeSpan.Parse(_options.IntervalPerQuestion);
                    _assistant = _sp.GetRequiredNamedService<IAssistant>(_options.AssistantName);

                    if (CheckHold())
                    {
                        var utcNow = DateTime.UtcNow;

                        DateTime nextRunTime = GetNextRunTime(utcNow);

                        TimeSpan holdTime = nextRunTime - utcNow;

                        _logger.LogTrace("Holding for {HoldTime}", holdTime.ToString(@"d\.hh\:mm\:ss"));
                        await Task.Delay(holdTime, _ctsOptionsChanged.Token);
                    }

                    var questionResult = await _client.ListStringAsync(_options.QuestionQuery);

                    if (questionResult.Count > 0)
                    {
                        var question = questionResult[0];

                        await _assistant.AskAsync(question, new() { UserId = _options.Username });
                        _logger.LogTrace("Waiting for interval {Interval} at {CurrentTime}", _interval, DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"));
                        await Task.Delay(_interval, _ctsOptionsChanged.Token);
                    }
                    else
                    {
                        _logger.LogTrace("No questions available to ask.");
                        await Task.Delay(_interval, _ctsOptionsChanged.Token);
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
                    _logger.LogError(ex, $"Error in {nameof(ScheduledQuestionAgent)}: {ex.Message}");
                    await Task.Delay(1_000);
                }
            }
        });
    }

    public async Task StopAsync()
    {
        await _cts.CancelAsync();
        await _lock.DisposeAsync();
    }

    public bool IsRunning()
    {
        return _runningTask is { IsCompleted: false, IsCanceled: false };
    }
}
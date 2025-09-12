using System.Diagnostics;
using JetBrains.Annotations;

namespace CX.Engine.Common.Telemetry;

public sealed class WindowedDurationMetric : IMetric<TimeSpan>
{
    public readonly TimeSpan WindowDuration;
    public readonly WindowData[] Windows;
    private readonly long[] _windowNumbers;
    private readonly int _windowCount;
    private readonly object _lock = new();

    public int LifetimeObservations;
    public TimeSpan LifetimeTotal;

    public class WindowData
    {
        public TimeSpan Min = TimeSpan.MaxValue;
        public TimeSpan Max = TimeSpan.MinValue;
        public TimeSpan Total = TimeSpan.Zero;
        public int Observations;
    }

    public WindowedDurationMetric(TimeSpan windowDuration, int totalWindows)
    {
        WindowDuration = windowDuration;
        Windows = new WindowData[totalWindows];
        _windowNumbers = new long[totalWindows];
        for (var i = 0; i < totalWindows; i++)
        {
            Windows[i] = new();
            _windowNumbers[i] = -1; // Initialize to an invalid window number
        }

        _windowCount = totalWindows;
    }

    public void Observe(TimeSpan value, DateTime? timestamp)
    {
        lock (_lock)
        {
            timestamp ??= DateTime.UtcNow;
            LifetimeObservations++;
            LifetimeTotal += value;

            var observationTime = timestamp.Value;
            var windowNo = observationTime.Ticks / WindowDuration.Ticks;
            var currentWindowNo = DateTime.UtcNow.Ticks / WindowDuration.Ticks;

            var windowAge = currentWindowNo - windowNo;

            if (windowAge < 0)
                throw new InvalidOperationException("Future-dated observations not allowed");

            if (windowAge >= _windowCount)
            {
                // Observation is too old; ignore it
                return;
            }

            var windowIndex = (int)(windowNo % Windows.Length);

            // If the window is stale, reset it
            if (_windowNumbers[windowIndex] != windowNo)
            {
                _windowNumbers[windowIndex] = windowNo;
                Windows[windowIndex].Min = TimeSpan.MaxValue;
                Windows[windowIndex].Max = TimeSpan.MinValue;
                Windows[windowIndex].Total = TimeSpan.Zero;
                Windows[windowIndex].Observations = 0;
            }

            // Update the window data
            var window = Windows[windowIndex];
            if (value < window.Min) window.Min = value;
            if (value > window.Max) window.Max = value;
            window.Total += value;
            window.Observations++;
        }
    }

    public Snapshot GetSnapshot()
    {
        lock (_lock)
        {
            var now = DateTime.UtcNow;
            var currentWindowNo = now.Ticks / WindowDuration.Ticks;
            TimeSpan? min = null;
            TimeSpan? max = null;
            var total = TimeSpan.Zero;
            var observations = 0;

            for (var i = 0; i < Windows.Length; i++)
            {
                if (_windowNumbers[i] < 0)
                    continue; // Window was never used

                // Check if the window is still valid
                var windowAge = currentWindowNo - _windowNumbers[i];
                if (windowAge >= 0 && windowAge < _windowCount)
                {
                    // Window is valid
                    var window = Windows[i];
                    if (window.Observations > 0)
                    {
                        if (!min.HasValue || window.Min < min) min = window.Min;
                        if (!max.HasValue || window.Max > max) max = window.Max;
                        total += window.Total;
                        observations += window.Observations;
                    }
                }
            }

            return new()
            {
                WindowedMin = min,
                WindowedMax = max,
                WindowedAverage = observations == 0 ? null : total / observations,
                LifetimeTotal = LifetimeTotal,
                LifetimeObservations = LifetimeObservations
            };
        }
    }

    [PublicAPI]
    public class Snapshot
    {
        public TimeSpan? WindowedMin { get; set; }
        public TimeSpan? WindowedMax { get; set; }
        public TimeSpan LifetimeTotal { get; set; }
        public int LifetimeObservations { get; set; }
        public TimeSpan? WindowedAverage { get; set; }
    }

    public void Observe(TimeSpan value) => Observe(value, DateTime.UtcNow);

    public WindowedDurationMetricDisposable StartObserve() => new WindowedDurationMetricDisposable(this);

    public class WindowedDurationMetricDisposable : IDisposable
    {
        public readonly WindowedDurationMetric Metric;
        public readonly Stopwatch Stopwatch;
        public readonly DateTime Start;

        public WindowedDurationMetricDisposable(WindowedDurationMetric metric)
        {
            Metric = metric;
            Stopwatch = Stopwatch.StartNew();
            Start = DateTime.UtcNow;
        }

        public void Dispose()
        {
            Metric.Observe(Stopwatch.Elapsed, Start);
        }
    }
}

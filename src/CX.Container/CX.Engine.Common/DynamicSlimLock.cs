using CX.Engine.Common;
using CX.Engine.Common.Tracing;

public class DynamicSlimLock
{
    private int _maxCount;
    private int _currentCount;
    private readonly object _lock = new();
    private readonly Queue<TaskCompletionSource> _waitQueue = new();

    public DynamicSlimLock(int maxCount)
    {
        if (maxCount <= 0)
            throw new ArgumentException("Maximum count must be positive.");

        _maxCount = maxCount;
    }

    public async ValueTask WaitAsync()
    {
        TaskCompletionSource tcs;

        lock (_lock)
        {
            if (_currentCount < _maxCount)
            {
                _currentCount++;
                return;
            }

            tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
            _waitQueue.Enqueue(tcs);
        }

        await tcs.Task.ConfigureAwait(false);
    }

    public async Task<DynamicSlimLockDisposable> UseAsync()
    {
        await WaitAsync();
        return new(this);
    }

    public async Task<DynamicSlimLockDisposable> UseWithTraceAsync()
    {
        await WaitWithTraceAsync();
        return new(this);
    }

    public void Release()
    {
        TaskCompletionSource toRelease = null;

        lock (_lock)
        {
            if (_currentCount <= 0)
                throw new SemaphoreFullException("Cannot release semaphore that is not acquired.");

            _currentCount--;

            if (_waitQueue.Count > 0 && _currentCount < _maxCount)
            {
                toRelease = _waitQueue.Dequeue();
                _currentCount++;
            }
        }

        toRelease?.SetResult();
    }

    public void SetMaxCount(int newMaxCount)
    {
        if (_maxCount == newMaxCount)
            return;
        
        if (newMaxCount <= 0)
            throw new ArgumentException("Maximum count must be positive.");

        var toRelease = new List<TaskCompletionSource>();

        lock (_lock)
        {
            var oldMaxCount = _maxCount;
            _maxCount = newMaxCount;

            if (_maxCount > oldMaxCount)
            {
                while (_waitQueue.Count > 0 && _currentCount < _maxCount)
                {
                    var tcs = _waitQueue.Dequeue();
                    toRelease.Add(tcs);
                    _currentCount++;
                }
            }
        }

        // Release outside of lock
        foreach (var tcs in toRelease)
            tcs.SetResult();
    }

    public async ValueTask WaitWithTraceAsync()
    {
        await CXTrace.Current.SpanFor(CXTrace.Section_Queue, new
        {
            // ReSharper disable once InconsistentlySynchronizedField
            CurrentCount = _currentCount,
            // ReSharper disable once InconsistentlySynchronizedField
            MaxCount = _maxCount
        }).ExecuteAsync(async _ => await WaitAsync());
    }
}

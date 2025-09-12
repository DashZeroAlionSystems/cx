namespace CX.Engine.Common;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public class OrderedSemaphoreSlim
{
    private int _count;
    private readonly Queue<TaskCompletionSource<bool>> _waiters = new();
    private readonly object _lock = new();

    public OrderedSemaphoreSlim(int initialCount)
    {
        if (initialCount < 0)
            throw new ArgumentOutOfRangeException(nameof(initialCount), "Initial count must be non-negative.");
        _count = initialCount;
    }

    public Task WaitAsync()
    {
        lock (_lock)
        {
            if (_count > 0)
            {
                _count--;
                return Task.CompletedTask;
            }
            else
            {
                var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                _waiters.Enqueue(tcs);
                return tcs.Task;
            }
        }
    }
    
    /// <summary>
    /// Designed for managing SlimLocks with a using statement.
    /// </summary>
    public async ValueTask<OrderedSemaphoreSlimDisposable> UseAsync()
    {
        await WaitAsync();
        return new(this);
    }


    public void Release()
    {
        TaskCompletionSource<bool> toRelease = null;
        lock (_lock)
        {
            if (_waiters.Count > 0)
            {
                toRelease = _waiters.Dequeue();
            }
            else
            {
                _count++;
            }
        }
        toRelease?.SetResult(true);
    }
}

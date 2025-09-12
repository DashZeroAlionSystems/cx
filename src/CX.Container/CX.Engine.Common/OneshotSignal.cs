using System.Runtime.CompilerServices;

namespace CX.Engine.Common;

/// <summary>
/// A signal that will only fire once.
/// </summary>
public readonly struct OneshotSignal
{
    private readonly TaskCompletionSource _tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
    
    public Task AsTask => _tcs.Task;

    public OneshotSignal()
    {
    }

    /// <summary>
    /// Fires the signal.  Once it is fired, awaiting this object will always complete immediately.
    /// </summary>
    public void Fire()
    {
        _tcs.TrySetResult();
    }

    public async void OnFire(Action a)
    {
        await this;
        a?.Invoke();
    }

    public async void FiresWhen(OneshotSignal? signal)
    {
        if (signal == null)
            return;
        
        await signal;
        Fire();
    }

    public OneshotSignal Or(OneshotSignal? other)
    {
        if (other == null)
            return this;

        var me = this;
        
        async void Handle(OneshotSignal res)
        {
            await Task.WhenAny(res, other.Value);
            res.Fire();
        }

        var res = new OneshotSignal();
        Handle(res);
        return res;
    }

    public OneshotSignal And(OneshotSignal? other)
    {
        if (other == null)
            return this;

        var me = this;
        
        async void Handle(OneshotSignal res)
        {
            await me;
            await other.Value;
            res.Fire();
        }

        var res = new OneshotSignal();
        Handle(res);
        return res;
    }

    public TaskAwaiter GetAwaiter() => _tcs.Task.GetAwaiter();

    public static OneshotSignal ForTask(Task t)
    {
        var signal = new OneshotSignal();
        t.ContinueWith(_ => signal.Fire());
        return signal;
    }

    //define a and operator
    public static OneshotSignal operator &(OneshotSignal a, OneshotSignal? b)
    {
        return a.And(b);
    }
    
    public static OneshotSignal operator |(OneshotSignal a, OneshotSignal? b)
    {
        return a.Or(b);
    }
    
    public static implicit operator Task(OneshotSignal? a) => a?.AsTask ?? Task.CompletedTask;

    public static implicit operator OneshotSignal?(Task t) => ForTask(t);
}

public static class OneshotSignalExts
{
    public static void Fire(this OneshotSignal? signal) => signal?.Fire();
    public static void OnFire(this OneshotSignal? signal, Action a) => signal?.OnFire(a);

    public static async Task WaitForAndHoldUntilAsync(this SemaphoreSlim semaphore, OneshotSignal? signal)
    {
        if (semaphore != null && signal != null)
        {
            await semaphore.WaitAsync();

            if (signal != null)
                signal.Value.AsTask.ContinueWith(_ => { semaphore.Release(); });
            else
                semaphore.Release();
        }
    }

    public static void FiresWhen(this OneshotSignal? a, OneshotSignal? b)
    {
        if (a != null && b != null)
            a.Value.FiresWhen(b);
    }

    public static TaskAwaiter GetAwaiter(this OneshotSignal? signal)
    {
        if (signal == null)
            return Task.CompletedTask.GetAwaiter();

        return signal.Value.GetAwaiter();
    }
}     
    

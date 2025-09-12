using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

namespace CX.Engine.Common;

/// <summary>
/// Waits for a Snapshot to be set.  Raises an exception if there is no valid snapshot.
/// </summary>
public class WaitForSnapshotTask
{
    private readonly TaskCompletionSource _tcsWaiting = new();
    private Task _taskWaiting;

    public TaskAwaiter GetAwaiter() => _taskWaiting.GetAwaiter();
    
    public WaitForSnapshotTask()
    {
        _taskWaiting = _tcsWaiting.Task;
    }

    private void OnValidSnapshotReceived()
    {
        _tcsWaiting.TrySetResult();
        _taskWaiting = Task.CompletedTask;
    }

    private void OnException(Exception ex)
    {
        _tcsWaiting.TrySetException(ex);
    }

    public async Task Do(ILogger logger, Action setSnapshot)
    {
        if (setSnapshot == null)
            throw new ArgumentNullException(nameof(setSnapshot));

        try
        {
            setSnapshot();
            OnValidSnapshotReceived();
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Error trying to set snapshot.  Snapshot will be ignored.");
            OnException(ex);
        }
    }

    public async void DoAsync(ILogger logger, Func<Task> setSnapshotAsync)
    {
        if (setSnapshotAsync == null)
            throw new ArgumentNullException(nameof(setSnapshotAsync));

        try
        {
            await setSnapshotAsync();
            OnValidSnapshotReceived();
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Error trying to set snapshot.  Snapshot will be ignored.");
            OnException(ex);
        }
    }
}
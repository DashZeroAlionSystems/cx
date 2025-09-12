using CX.Engine.Common.Tracing;
using Microsoft.Extensions.Logging;

namespace CX.Engine.Common.DistributedLocks;

internal sealed class PollingDistributedLock : IAsyncDisposable
{
    private readonly object _lock = new();
    private bool _held;
    private bool _acquiring;
    private bool _disposing;
    private readonly DistributedLockService _lockService;
    internal readonly string Id;

    internal PollingDistributedLock(DistributedLockService lockService, string id)
    {
        _lockService = lockService;
        Id = id ?? throw new ArgumentNullException(nameof(id));
    }

    public async Task UseAsync(CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            if (_acquiring)
                throw new InvalidOperationException("Lock is already being acquired");

            if (_disposing)
                throw new InvalidOperationException("Object is already being disposed");

            _acquiring = true;
        }

        await CXTrace.Current.SpanFor(CXTrace.Section_AcquireDistributedLock, null).ExecuteAsync(async _ =>
        {
            while (true)
            {
                try
                {
                    bool acquired;

                    using (var __ = await DistributedLockService.PollSemaphoreSlim.UseAsync())
                        acquired = await _lockService.Internal_TryAcquireLockAsync(Id);

                    if (!acquired)
                    {
                        if (cancellationToken.IsCancellationRequested)
                            cancellationToken.ThrowIfCancellationRequested();

                        await Task.Delay(_lockService.Options.AcquirePollingInterval);

                        if (cancellationToken.IsCancellationRequested)
                            cancellationToken.ThrowIfCancellationRequested();

                        continue;
                    }
                }
                // We have to retry on any exceptions here to avoid hung locks that survive till our process dies.
                // This can not happen infinitely since any truly non-recoverable scenario will kill our process from the coordinator quickly.
                catch (Exception ex)
                {
                    _lockService.Logger.LogError(ex, $"Retrying to acquire lock {Id} in {_lockService.Options.AcquirePollingInterval}");
                    await Task.Delay(_lockService.Options.AcquirePollingInterval);
                    continue;
                }

                lock (_lock)
                {
                    if (_disposing)
                        throw new InvalidOperationException(
                            "Corrupt distributed state: Distributed lock has been disposed before being acquired.");

                    if (!_acquiring)
                        throw new InvalidOperationException("Lock is being acquired but _acquiring = false");

                    if (_held)
                        throw new InvalidOperationException("Lock is being acquired but _held = false");

                    _held = true;
                    _acquiring = false;
                }

                break;
            }
        });
    }

    public async ValueTask DisposeAsync()
    {
        bool held;

        lock (_lock)
        {
            _disposing = true;
            held = _held;
            _held = false;
        }

        if (held)
            await CXTrace.Current.SpanFor(CXTrace.Section_ReleaseDistributedLock, null).ExecuteAsync(async _ =>
            {
                await _lockService.Internal_ReleaseAsync(Id);
            });
    }
}
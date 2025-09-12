using System.Diagnostics;
using CX.Engine.Common.PostgreSQL;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;

namespace CX.Engine.Common.DistributedLocks;

/// <summary>
/// NB: Methods in this class are not cancellation-safe.  Cancellations can lead to distributed issues, like locks being held for the lifetime of the service.
/// Do not call methods starting with Internal directly.  Adhere to the patterns employed by this class to use it safely. 
/// </summary>
public sealed class DistributedLockService : IHostedService
{
    public const int LockFailExitCode = 65;

    internal readonly ILogger<DistributedLockService> Logger;
    internal readonly DistributedLockServiceOptions Options;

    //The static variables here work around an issue where singletons are not honored in the production environment due to MediatR running it's own service provider.
    internal static readonly SemaphoreSlim PollSemaphoreSlim = new(1, 1);

    private readonly PostgreSQLClient _sql;
    private readonly CancellationTokenSource _ctsStopped = new();
    private static readonly KeyedSemaphoreSlim _localProcessLock = new();
    private readonly TaskCompletionSource _tcsServiceIdAcquired = new();
    public Task ServiceIdAcquired => _tcsServiceIdAcquired.Task;

    //The static variables here work around an issue where singletons are not honored in the production environment due to MediatR running it's own service provider.
    public static Guid? ServiceId { get; private set; }

    public DistributedLockService(IOptions<DistributedLockServiceOptions> options, IServiceProvider sp,
        ILogger<DistributedLockService> logger)
    {
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        Options = options?.Value ?? throw new ArgumentNullException(nameof(options));

        if (Options.DebugMode)
        {
            Options.RenewInterval = TimeSpan.FromMinutes(5);
        }

        Options.Validate();
        _sql = sp.GetRequiredNamedService<PostgreSQLClient>(Options.PostgreSQLClientName);
    }
    
    private void KillImmediately(string reason)
    {
        Logger.LogCritical(
            $"Failed to renew distributed lock.  Reason: {reason}\nTerminating service with immediate effect.");
        Environment.Exit(LockFailExitCode);
        throw new InvalidOperationException("NB: This line of code or any other in the process will never run.");
    }

    private async void StartOwnLockRenewer()
    {
        while (true)
        {
            int success;

            try
            {
                var sw = Stopwatch.StartNew();
                var timeoutTask = Task.Delay(Options.RenewInterval);
                //We have to only renew locks that have not expired yet according to the database server.
                //If a lock has already expired we or another service might be cleaning up its resources and renewing it will lead to race conditions.
                var successTask = _sql.ExecuteAsync<int>(
                    $"UPDATE DistributedLockServiceInstances SET Expires = now() + {Options.ExpiryInterval} WHERE Id = {ServiceId} AND Expires > now()");
                await Task.WhenAny(timeoutTask, successTask);
                if (sw.Elapsed > Options.RenewInterval * 2)
                {
                    KillImmediately(
                        "TPL congestion or process suspension detected during renew interval.  Distributed locks do not handle this safely, developers should resolve ASAP.");
                    return;
                }

                success = successTask.IsCompleted ? successTask.Result : -1;
            }
            catch (Exception ex)
            {
                KillImmediately(ex.ToString());
                return;
            }

            if (success != 1)
                KillImmediately(success == -1 ? "Timeout" : "Lock already expired");

            {
                var sw = Stopwatch.StartNew();
                await Task.Delay(Options.LockInterval);
                if (sw.Elapsed > Options.LockInterval + Options.RenewInterval)
                    KillImmediately(
                        "TPL congestion or process suspension detected during lock interval.  Distributed locks do not handle this safely, developers should resolve ASAP.");
            }
        }
        // ReSharper disable once FunctionNeverReturns
    }

    private async void StartCleanupService()
    {
        while (!_ctsStopped.IsCancellationRequested)
        {
            try
            {
                while (true)
                {
                    var id = await _sql.ScalarAsync<Guid?>(
                        "SELECT Id FROM DistributedLockServiceInstances WHERE Expires < now() ORDER BY RANDOM() LIMIT 1");

                    if (id is null)
                        break;

                    Logger.LogDebug($"Cleaning up expired service {id}");

                    await _sql.ExecuteAsync($"DELETE FROM DistributedLocks WHERE ServiceId = {id}");
                    await _sql.ExecuteAsync($"DELETE FROM DistributedLockServiceInstances WHERE Id = {id}");
                    continue;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"Error in cleanup service.  Retrying in {Options.CheckInterval}");
            }

            await Task.Delay(Options.CheckInterval);
        }
    }

    public Task<IAsyncDisposable> UseAsync<T>(string lockId, CancellationToken cancellationToken = default) => UseAsync(
        $"{typeof(T).Name}://{lockId}");

    public async Task<IAsyncDisposable> UseAsync(string lockId, CancellationToken cancellationToken = default)
    {
        if (lockId.Length > 100)
            throw new InvalidOperationException("Lock ID must be less than 100 characters");

        var lockObj = new PollingDistributedLock(this, lockId);
        try
        {
            await lockObj.UseAsync(cancellationToken);
        }
        catch
        {
            await lockObj.DisposeAsync();
            throw;
        }

        return lockObj;
    }

    internal async Task Internal_ReleaseAsync(string lockId)
    {
        if (!_localProcessLock.IsHeld(lockId))
            throw new InvalidOperationException($"Distributed Lock {lockId} is not currently held by this process");
        
        //Infinite retries are necessary to guarantee release under transient network circumstances.
        //This is not truly infinite, since it can only continue to occur as long as our Service ID has not expired.
        while (true)
        {
            try
            {
                await _sql.ExecuteAsync(
                    $"""
                     DELETE FROM DistributedLocks WHERE Id = {lockId} AND ServiceId = {ServiceId}
                     """);
                _localProcessLock.Release(lockId);
                break;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"Retrying to release lock {lockId} in {Options.AcquirePollingInterval}");
                await Task.Delay(Options.AcquirePollingInterval);
                continue;
            }
        }
    }

    internal async Task<bool> Internal_TryAcquireLockAsync(string lockId)
    {
        if (ServiceId == null)
            throw new InvalidOperationException("ServiceId not set yet (wait for StartAsync to finish)");

        var success = false;
        await _localProcessLock.WaitAsync(lockId);
        try
        {
            var res = await _sql.ScalarAsync<int>(
                $"""
                 WITH ins AS (
                     INSERT INTO DistributedLocks (Id, ServiceId)
                     VALUES ({lockId}, {ServiceId})
                     ON CONFLICT (Id) DO NOTHING
                     RETURNING 1
                 )
                 SELECT 
                     CASE 
                         WHEN (SELECT 1 FROM ins) = 1 THEN 1
                         WHEN EXISTS (SELECT 1 FROM DistributedLocks WHERE Id = {lockId} AND ServiceId = {ServiceId}) THEN 1
                         ELSE 0
                     END AS InsertStatus
                 """);

            success = res == 1 || Options.DebugMode;
        }
        finally
        {
            if (!success)
                _localProcessLock.Release(lockId);
        }

        return success;
    }

    private bool _started;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (_started)
            return;

        _started = true;

        Logger.LogInformation("Initializing...");
        await _sql.ExecuteAsync(
            """
            CREATE TABLE IF NOT EXISTS DistributedLockServiceInstances(
                   Id UUID PRIMARY KEY,
                   Expires TIMESTAMP WITH TIME ZONE NOT NULL
            )
            """);

        await _sql.ExecuteAsync(
            """
            CREATE TABLE IF NOT EXISTS DistributedLocks(
                Id varchar(100) PRIMARY KEY,
                ServiceId UUID,
                CONSTRAINT fk_serviceid
                    FOREIGN KEY (ServiceId)
                    REFERENCES DistributedLockServiceInstances(Id)
                    ON DELETE CASCADE
            );
            """);

        await _sql.ExecuteAsync("CREATE INDEX IF NOT EXISTS idx_serviceid ON DistributedLocks(ServiceId);");

        Logger.LogInformation("Reserving instance ID...");
        while (true)
        {
            ServiceId = Guid.NewGuid();
            try
            {
                await _sql.ExecuteAsync(
                    $"INSERT INTO DistributedLockServiceInstances (Id, Expires) SELECT {ServiceId}, now() + {Options.ExpiryInterval}");
                break;
            }
            //retry on duplicate
            catch (NpgsqlException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
            {
                ServiceId = null;
                continue;
            }
        }

        StartOwnLockRenewer();
        StartCleanupService();

        Logger.LogInformation($"Distributed Lock Service started with Service ID {ServiceId}.");
        _tcsServiceIdAcquired.TrySetResult();
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _ctsStopped.Cancel();
        return Task.CompletedTask;
    }
}
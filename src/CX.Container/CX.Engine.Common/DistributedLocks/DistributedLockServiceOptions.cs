using JetBrains.Annotations;

namespace CX.Engine.Common.DistributedLocks;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class DistributedLockServiceOptions
{
    public string PostgreSQLClientName { get; set; } = null!;
    
    /// <summary>
    /// Time between attempts to renew the lock.
    /// </summary>
    public TimeSpan LockInterval { get; set; }
    /// <summary>
    /// Timeout for the renewal query and its retries.
    /// This is also the maximum amount of TPL congestion that the distributed locking system will tolerate.
    /// </summary>
    public TimeSpan RenewInterval { get; set; }
    /// <summary>
    /// Grace period after renewal has failed before another service takes over.
    /// This should be larger than the time needed for the killed or hanging service to relinquish held resources.
    /// Also, when the TPL stalls the service holding the lock has at least this amount of time to avoid failure by renewing its lock.
    /// </summary>
    public TimeSpan GraceInterval { get; set; }
    
    /// <summary>
    /// Time between checks for expired service handles. 
    /// </summary>
    public TimeSpan CheckInterval { get; set; }
    
    public TimeSpan ExpiryInterval { get; set; }
    
    public TimeSpan AcquirePollingInterval { get; set; }

    /// <summary>
    /// Designed to handle debuggers pausing.
    /// When enabled: Renew interval set to 5 minutes.  Acquiring locks always succeeds.
    /// </summary>
    public bool DebugMode { get; set; }

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(PostgreSQLClientName))
            throw new InvalidOperationException($"{nameof(DistributedLockServiceOptions)}.{nameof(PostgreSQLClientName)} is required");
        
        if (LockInterval.TotalSeconds < 1)
            throw new InvalidOperationException($"{nameof(DistributedLockServiceOptions)}.{nameof(LockInterval)} must be at least 1 second");
        
        if (RenewInterval.TotalSeconds < 1)
            throw new InvalidOperationException($"{nameof(DistributedLockServiceOptions)}.{nameof(RenewInterval)} must be at least 1 second");
        
        if (GraceInterval.TotalSeconds < 1)
            throw new InvalidOperationException($"{nameof(DistributedLockServiceOptions)}.{nameof(GraceInterval)} must be at least 1 second");

        if (CheckInterval.TotalSeconds < 1)
            throw new InvalidOperationException($"{nameof(DistributedLockServiceOptions)}.{nameof(CheckInterval)} must be at least 1 second");
        
        if (AcquirePollingInterval.TotalSeconds < 1)
            throw new InvalidOperationException($"{nameof(DistributedLockServiceOptions)}.{nameof(AcquirePollingInterval)} must be at least 1 second");

        ExpiryInterval = LockInterval + RenewInterval + GraceInterval;
    }
}

using JetBrains.Annotations;

namespace CX.Engine.Common.Telemetry;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class PgTelemetryRecorderOptions : IValidatable
{
    public bool Enabled { get; set; }
    public TimeSpan LoopInterval { get; set; }
    
    public bool CleanupEachLoop { get; set; } = true;
    public int CleanupOlderThanDays { get; set; } = 1;
    
    public string PostgreSQLClientName { get; set; }

    public void Validate()
    {
        if (!Enabled)
            return;
        
        if (LoopInterval.TotalMilliseconds < 1)
            throw new InvalidOperationException($"{nameof(LoopInterval)} must be at least 1 millisecond");
        
        if (string.IsNullOrWhiteSpace(PostgreSQLClientName))
            throw new InvalidOperationException($"{nameof(PostgreSQLClientName)} must be set");
        
        if (CleanupEachLoop && CleanupOlderThanDays < 1)
            throw new InvalidOperationException($"{nameof(CleanupOlderThanDays)} must be at least 1 day if {nameof(CleanupEachLoop)} is true"); }
}
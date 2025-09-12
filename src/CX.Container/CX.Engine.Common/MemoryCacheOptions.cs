namespace CX.Engine.Common;

public class MemoryCacheOptions
{
    public TimeSpan? EntriesExpiresAfterNoAccessDuration { get; set; }
    public TimeSpan? ExpiryCheckInterval { get; set; }
    public TimeSpan? EntriesExpiresAfterCreation {get;set;}
    
    public void Validate()
    {
        if (EntriesExpiresAfterNoAccessDuration != null && EntriesExpiresAfterNoAccessDuration.Value < TimeSpan.Zero)
            throw new ArgumentException($"{nameof(EntriesExpiresAfterNoAccessDuration)} must be greater than or equal to zero.");
        
        if (ExpiryCheckInterval != null && ExpiryCheckInterval.Value < TimeSpan.Zero)
            throw new ArgumentException($"{nameof(ExpiryCheckInterval)} must be greater than or equal to zero.");
        
        if (EntriesExpiresAfterCreation != null && EntriesExpiresAfterCreation.Value < TimeSpan.Zero)
            throw new ArgumentException($"{nameof(EntriesExpiresAfterCreation)} must be greater than or equal to zero.");
    }
}
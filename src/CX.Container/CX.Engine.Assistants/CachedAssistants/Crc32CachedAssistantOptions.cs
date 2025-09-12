using CX.Engine.Common;

namespace CX.Engine.Assistants.CachedAssistants;

public class Crc32CachedAssistantOptions : IValidatable
{
    public string UnderlyingAssistantName { get; set; }
    public string CachePostgreSQLClientName { get; set; }
    public string CacheTableName { get; set; }
    
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(UnderlyingAssistantName))
            throw new InvalidOperationException($"{nameof(UnderlyingAssistantName)} is required.");
        
        if (string.IsNullOrWhiteSpace(CachePostgreSQLClientName))
            throw new InvalidOperationException($"{nameof(CachePostgreSQLClientName)} is required.");
        
        if (string.IsNullOrWhiteSpace(CacheTableName))
            throw new InvalidOperationException($"{nameof(CacheTableName)} is required.");
    }
}
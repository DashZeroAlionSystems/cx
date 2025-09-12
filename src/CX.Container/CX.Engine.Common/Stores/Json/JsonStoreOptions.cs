using JetBrains.Annotations;

namespace CX.Engine.Common.Stores.Json;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class JsonStoreOptions
{
    public string PostgreSQLClientName { get; set; } = null!;
    public string TableName { get; set; } = null!;
    public int KeyLength { get; set; }
    
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(PostgreSQLClientName))
            throw new InvalidOperationException($"{nameof(JsonStoreOptions)}.{nameof(PostgreSQLClientName)} must be set");
        
        if (string.IsNullOrWhiteSpace(TableName))
            throw new InvalidOperationException($"{nameof(JsonStoreOptions)}.{nameof(TableName)} must be set");
        
        if (KeyLength < 1)
            throw new InvalidOperationException($"{nameof(JsonStoreOptions)}.{nameof(KeyLength)} must be at least 1");
    }
}
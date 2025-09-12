using JetBrains.Annotations;

namespace CX.Engine.Common.Stores.Json;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class JsonObjectStoreOptions : IValidatable
{
    public string PostgreSQLClientName { get; set; }
    public string TableName { get; set; }
    public int MaxKeyLength { get; set; } = 100;

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(PostgreSQLClientName))
            throw new InvalidOperationException($"{nameof(PostgreSQLClientName)} is required.");
        
        if (string.IsNullOrWhiteSpace(TableName))
            throw new InvalidOperationException($"{nameof(TableName)} is required.");
        
        if (MaxKeyLength < 1)
            throw new InvalidOperationException($"{nameof(MaxKeyLength)} must be greater than 0.");
    }
}
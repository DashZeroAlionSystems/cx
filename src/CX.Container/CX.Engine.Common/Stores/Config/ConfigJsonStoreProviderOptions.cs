namespace CX.Engine.Common.Stores.Json;

public class ConfigJsonStoreProviderOptions : IValidatable
{
    public TimeSpan RetryDelay { get; set; }
    public TimeSpan RefreshInterval { get; set; }
    public string PostgreSQLClientName { get; set; } = null!;

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(PostgreSQLClientName))
            throw new ArgumentException($"{nameof(ConfigJsonStoreProviderOptions)}.{nameof(PostgreSQLClientName)} must be set.");
    }
}
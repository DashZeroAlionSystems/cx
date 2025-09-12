using JetBrains.Annotations;

namespace CX.Engine.Common.PostgreSQL;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class PostgreSQLClientOptions : IValidatable
{
    public string ConnectionString { get; set; } = null!;
    public int MaxConcurrentQueries { get; set; }

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(ConnectionString))
            throw new InvalidOperationException($"{nameof(PostgreSQLClientOptions)}.{nameof(ConnectionString)} must be set");

        if (MaxConcurrentQueries < 1)
            throw new InvalidOperationException($"{nameof(PostgreSQLClientOptions)}.{nameof(MaxConcurrentQueries)} must be at least 1");
    }
}
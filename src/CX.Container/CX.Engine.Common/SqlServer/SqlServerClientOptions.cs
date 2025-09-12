using JetBrains.Annotations;

namespace CX.Engine.Common.SqlServer;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class SqlServerClientOptions : IValidatable
{
    public string ConnectionString { get; set; } = null!;
    public int MaxConcurrentQueries { get; set; }

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(ConnectionString))
            throw new InvalidOperationException($"{nameof(SqlServerClientOptions)}.{nameof(ConnectionString)} must be set");

        if (MaxConcurrentQueries < 1)
            throw new InvalidOperationException($"{nameof(SqlServerClientOptions)}.{nameof(MaxConcurrentQueries)} must be at least 1");
    }
}
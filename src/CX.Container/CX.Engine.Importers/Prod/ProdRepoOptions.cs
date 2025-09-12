namespace CX.Engine.Importing.Prod;

public class ProdRepoOptions
{
    public string PostgreSQLClientName { get; set; } = null!;

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(PostgreSQLClientName))
            throw new ArgumentException($"{nameof(ProdRepoOptions)}.{nameof(PostgreSQLClientName)} is required");
    }
}
namespace CX.Engine.Common.JsonSchemas;

public class JsonSchemaStoreOptions : IValidatable
{
    public string PostgreSQLClientName { get; set; }
    
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(PostgreSQLClientName))
            throw new InvalidOperationException($"{nameof(PostgreSQLClientName)} is required");
    }
}
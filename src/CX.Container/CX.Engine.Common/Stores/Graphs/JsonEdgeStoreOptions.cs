using System.ComponentModel.DataAnnotations;

namespace CX.Engine.Common.Stores.Graphs;

public class JsonEdgeStoreOptions : IValidatable
{
    public string PostgreSQLClientName { get; set; }
    public string TableName { get; set; }
    public int MaxKeyLength { get; set; } = 100;

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(PostgreSQLClientName))
            throw new ValidationException($"{nameof(PostgreSQLClientName)} is required.");
        
        if (string.IsNullOrWhiteSpace(TableName))
            throw new ValidationException($"{nameof(TableName)} is required.");
        
        if (MaxKeyLength < 1)
            throw new ValidationException($"{nameof(MaxKeyLength)} must be greater than 0.");
    }
}
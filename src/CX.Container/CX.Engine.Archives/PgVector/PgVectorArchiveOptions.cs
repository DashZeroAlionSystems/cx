using System.ComponentModel.DataAnnotations;

namespace CX.Engine.Archives.PgVector;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class PgVectorArchiveOptions : IValidatable
{
    public string PgClientName { get; set; }
    public string TableName { get; set; } = "vector_store";
    public string EmbeddingModel { get; set; }
    public int EmbeddingLength { get; set; } = 1536;
    public int KeyLength { get; set; } = 100;
    public int MaxChunksPerQuery { get; set; } = 100;

    public bool AutoEnablePgVectorExtension { get; set; } = true;

    public bool AutoCreateTable { get; set; } = true;
    public bool AutoCreateIVFFlatCosineIndex { get; set; } = true;
    public string AttachmentsBaseUrl { get; set; }

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(PgClientName))
            throw new ValidationException($"{nameof(PgClientName)} is required");
        
        if (string.IsNullOrWhiteSpace(TableName))
            throw new ValidationException($"{nameof(TableName)} is required");
        
        if (string.IsNullOrWhiteSpace(EmbeddingModel))
            throw new ValidationException($"{nameof(EmbeddingModel)} is required");

        if (EmbeddingLength < 1)
            throw new ValidationException($"{nameof(EmbeddingLength)} must be greater than 0");
        
        if (EmbeddingLength > 2000)
            throw new ValidationException($"{nameof(EmbeddingLength)} must be less than 2000 (pg_vector limitation)");
        
        if (KeyLength < 1)
            throw new ValidationException($"{nameof(KeyLength)} must be greater than 0");
        
        if (KeyLength > 2704)
            throw new ValidationException($"{nameof(KeyLength)} must be less than 2704 (PostgreSQL 15 index limitation)");
        
        if (MaxChunksPerQuery < 1)
            throw new ValidationException($"{nameof(MaxChunksPerQuery)} must be greater than 0");
    }
}
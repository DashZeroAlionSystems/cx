using CX.Engine.Common.Embeddings.OpenAI;

namespace CX.Engine.Archives.Pinecone;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class PineconeOptions : IValidatable
{
    public string APIKey { get; set; } = null!;
    public string IndexName { get; set; } = null!;
    public string Namespace { get; set; } = null!;
    public string EmbeddingModel { get; set; } = null!;
    public bool? UseJsonVectorTracker { get; set; }    
    public string JsonVectorTrackerName { get; set; }
    public string AttachmentsBaseUrl { get; set; }
    public int MaxConcurrency { get; set; }
    public int MaxChunksPerPineconeQuery { get; set; }
     
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(APIKey))
            throw new InvalidOperationException($"{nameof(PineconeOptions)}.{nameof(APIKey)} is required.");
        
        if (Namespace == null)
            throw new InvalidOperationException($"{nameof(PineconeOptions)}.{nameof(Namespace)} is required.");
        
        if (string.IsNullOrWhiteSpace(IndexName))
            throw new InvalidOperationException($"{nameof(PineconeOptions)}.{nameof(IndexName)} is required.");
        
        if (string.IsNullOrWhiteSpace(EmbeddingModel))
            throw new InvalidOperationException($"{nameof(PineconeOptions)}.{nameof(EmbeddingModel)} is required.");
        
        if (AttachmentsBaseUrl == null)
            throw new InvalidOperationException($"{nameof(PineconeOptions)}.{nameof(AttachmentsBaseUrl)} is required.");
        
        if (!OpenAIEmbedder.IsValidModel(EmbeddingModel))
            throw new InvalidOperationException($"{nameof(PineconeOptions)}.{nameof(EmbeddingModel)} is not a valid OpenAI model.");
        
        if (MaxChunksPerPineconeQuery < 1)
            throw new InvalidOperationException($"{nameof(PineconeOptions)}.{nameof(MaxChunksPerPineconeQuery)} must be greater than 0.");

        if (MaxConcurrency < 1)
            throw new InvalidOperationException($"{nameof(PineconeOptions)}.{nameof(MaxConcurrency)} must be greater than 0.");

        if (!UseJsonVectorTracker.HasValue)
            throw new InvalidOperationException($"{nameof(PineconeOptions)}.{nameof(UseJsonVectorTracker)} is required.");

        if (UseJsonVectorTracker.Value && string.IsNullOrWhiteSpace(JsonVectorTrackerName))
            throw new InvalidOperationException($"{nameof(PineconeOptions)}.{nameof(JsonVectorTrackerName)} is required.");
    }
}
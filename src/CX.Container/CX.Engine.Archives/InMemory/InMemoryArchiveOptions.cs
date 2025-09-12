using CX.Engine.Common.Embeddings.OpenAI;

namespace CX.Engine.Archives.InMemory;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class InMemoryArchiveOptions
{
    public string Name = null!;
    public string EmbeddingModel { get; set; } = null!;
    
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(EmbeddingModel))
            throw new InvalidOperationException($"{nameof(InMemoryArchiveOptions)}.{nameof(EmbeddingModel)} is required.");

        if (!OpenAIEmbedder.IsValidModel(EmbeddingModel))
            throw new InvalidOperationException($"{nameof(InMemoryArchiveOptions)}.{nameof(EmbeddingModel)} must be a valid OpenAI model.");
    }
}
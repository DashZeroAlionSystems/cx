using JetBrains.Annotations;

namespace CX.Engine.Common.Embeddings.OpenAI;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class EmbeddingResponse
{
    public string Object { get; set; } = null!;
    public List<EmbeddingData> Data { get; set; } = null!;
    public string Model { get; set; } = null!;
    public UsageInfo Usage { get; set; } = null!;
}
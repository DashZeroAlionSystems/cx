using JetBrains.Annotations;

namespace CX.Engine.Common.Embeddings.OpenAI;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class EmbeddingData
{
    public string Object { get; set; } = null!;
    public int Index { get; set; }
    public List<float> Embedding { get; set; } = null!;
}
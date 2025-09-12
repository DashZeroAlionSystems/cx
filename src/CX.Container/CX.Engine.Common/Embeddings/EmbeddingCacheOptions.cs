using JetBrains.Annotations;

namespace CX.Engine.Common.Embeddings;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class EmbeddingCacheOptions
{
    public string CacheFile { get; set; }
    public bool UseCache { get; set; }
}
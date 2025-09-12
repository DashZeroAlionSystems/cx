using JetBrains.Annotations;

namespace CX.Engine.Common.Embeddings.OpenAI;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class OpenAIEmbedderOptions
{
    public string APIKey { get; set; } = null!;
    public int MaxConcurrentCalls { get; set; }
    public void Validate()
    {
        if (string.IsNullOrEmpty(APIKey))
            throw new ArgumentException($"{nameof(OpenAIEmbedderOptions)}.{nameof(APIKey)} is required");
        
        if (MaxConcurrentCalls <= 0)
            throw new ArgumentException($"{nameof(OpenAIEmbedderOptions)}.{nameof(MaxConcurrentCalls)} must be greater than 0");
    }
}
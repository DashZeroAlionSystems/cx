using JetBrains.Annotations;

namespace CX.Engine.TextProcessors;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class AzureAITranslatorOptions
{
    public string ApiKey { get; set; } = null!;
    public string Region { get; set; } = null!;
    public string TargetLanguage { get; set; } = null!;
    public bool? FailHard { get; set; }
    
    public double DontTranslateMinConfidence { get; set; }
    
    public int RetryMaxDelaySeconds { get; set; }
    public int RetryTimeoutSeconds { get; set; }
    
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(ApiKey))
            throw new InvalidOperationException($"{nameof(AzureAITranslatorOptions)}.{nameof(ApiKey)} is required.");
        
        if (string.IsNullOrWhiteSpace(TargetLanguage))
            throw new InvalidOperationException($"{nameof(AzureAITranslatorOptions)}.{nameof(TargetLanguage)} is required.");

        if (string.IsNullOrWhiteSpace(Region))
            throw new InvalidOperationException($"{nameof(AzureAITranslatorOptions)}.{nameof(Region)} is required.");
        
        if (!FailHard.HasValue)
            throw new InvalidOperationException($"{nameof(AzureAITranslatorOptions)}.{nameof(FailHard)} is required.");
        
        if (DontTranslateMinConfidence <= 0.01)
            throw new InvalidOperationException($"{nameof(AzureAITranslatorOptions)}.{nameof(DontTranslateMinConfidence)} must be greater than 0.01.");
        
        if (RetryMaxDelaySeconds <= 1)
            throw new InvalidOperationException($"{nameof(AzureAITranslatorOptions)}.{nameof(RetryMaxDelaySeconds)} must be greater than 1.");
        
        if (RetryTimeoutSeconds <= 1)
            throw new InvalidOperationException($"{nameof(AzureAITranslatorOptions)}.{nameof(RetryTimeoutSeconds)} must be greater than 1.");
    }
}
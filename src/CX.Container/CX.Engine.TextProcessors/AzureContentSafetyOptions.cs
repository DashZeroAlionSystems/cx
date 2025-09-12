using JetBrains.Annotations;

namespace CX.Engine.TextProcessors;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class AzureContentSafetyOptions
{
    public string Endpoint { get; set; } = null!;
    public string ApiKey { get; set; } = null!;

    public bool? FailHard { get; set; }

    public int RetryMaxDelaySeconds { get; set; }
    public int RetryTimeoutSeconds { get; set; }

    public int ExceptionHateLevel { get; set; }
    
    public int ExceptionSexualLevel { get; set; }
    
    public int ExceptionViolenceLevel { get; set; }
    
    public int ExceptionSelfHarmLevel { get; set; }
    
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Endpoint))
            throw new InvalidOperationException($"{nameof(AzureContentSafetyOptions)}.{nameof(Endpoint)} is required.");
        
        if (string.IsNullOrWhiteSpace(ApiKey))
            throw new InvalidOperationException($"{nameof(AzureContentSafetyOptions)}.{nameof(ApiKey)} is required.");
        
        if (!FailHard.HasValue)
            throw new InvalidOperationException($"{nameof(AzureContentSafetyOptions)}.{nameof(FailHard)} is required.");

        if (RetryMaxDelaySeconds <= 1)
            throw new InvalidOperationException($"{nameof(AzureContentSafetyOptions)}.{nameof(RetryMaxDelaySeconds)} must be greater than 1.");
        
        if (RetryTimeoutSeconds <= 1)
            throw new InvalidOperationException($"{nameof(AzureContentSafetyOptions)}.{nameof(RetryTimeoutSeconds)} must be greater than 1.");
        
        if (ExceptionHateLevel <= 0)
            throw new InvalidOperationException($"{nameof(AzureContentSafetyOptions)}.{nameof(ExceptionHateLevel)} must be greater than 0.");
        
        if (ExceptionSexualLevel <= 0)
            throw new InvalidOperationException($"{nameof(AzureContentSafetyOptions)}.{nameof(ExceptionSexualLevel)} must be greater than 0.");
        
        if (ExceptionViolenceLevel <= 0)
            throw new InvalidOperationException($"{nameof(AzureContentSafetyOptions)}.{nameof(ExceptionViolenceLevel)} must be greater than 0.");
        
        if (ExceptionSelfHarmLevel <= 0)
            throw new InvalidOperationException($"{nameof(AzureContentSafetyOptions)}.{nameof(ExceptionSelfHarmLevel)} must be greater than 0.");
        
    }
}
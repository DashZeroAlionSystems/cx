using JetBrains.Annotations;

namespace CX.Engine.DocExtractors.Text;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class MSDocAnalyzerOptions
{
    public string Endpoint { get; set; } = null!;
    public string APIKey { get; set; } = null!;
    public string BinaryStore { get; set; } = null!;
    
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Endpoint))
            throw new InvalidOperationException($"Missing {nameof(MSDocAnalyzerOptions)}.{nameof(Endpoint)}");
        
        if (string.IsNullOrWhiteSpace(APIKey))
            throw new InvalidOperationException($"Missing {nameof(MSDocAnalyzerOptions)}.{nameof(APIKey)}");
        
        if (string.IsNullOrWhiteSpace(BinaryStore))
            throw new InvalidOperationException($"Missing {nameof(MSDocAnalyzerOptions)}.{nameof(BinaryStore)}");
    }
}
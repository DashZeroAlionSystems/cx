using CX.Engine.Common;

namespace CX.Engine.CognitiveServices.ToxicityAnalysis;

public class ToxicityAnalyzerOptions : IValidatable
{
    public string Endpoint { get; set; }
    public string ApiKey { get; set; }
    public int CharacterLimit { get; set; }
    public int ThresholdLevel { get; set; } = 2;
    
    public void Validate()
    {
        if (CharacterLimit < 1)
            throw new InvalidOperationException($"{nameof(CharacterLimit)} must be greater than 0");
        
        if (string.IsNullOrWhiteSpace(Endpoint))
            throw new ArgumentException($"{nameof(Endpoint)} is required");
        
        if (string.IsNullOrWhiteSpace(ApiKey))
            throw new ArgumentException($"{nameof(ApiKey)} is required");
    }
}
using CX.Engine.Common;

namespace CX.Engine.CognitiveServices.SentimentAnalysis;

public class SentimentAnalyzerOptions : IValidatable
{
    public string Endpoint { get; set; }
    public string ApiKey { get; set; }
    public int CharacterLimit { get; set; }

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Endpoint))
            throw new ArgumentException($"{nameof(Endpoint)} is required");
        
        if (string.IsNullOrWhiteSpace(ApiKey))
            throw new ArgumentException($"{nameof(ApiKey)} is required");
        
        if (CharacterLimit < 1)
            throw new InvalidOperationException($"{nameof(CharacterLimit)} must be greater than 0");
    }
}
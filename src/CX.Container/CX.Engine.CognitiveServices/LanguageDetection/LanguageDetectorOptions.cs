using CX.Engine.Common;

namespace CX.Engine.CognitiveServices.LanguageDetection;

public class LanguageDetectorOptions : IValidatable
{
    public string Endpoint { get; set; }
    public string ApiKey { get; set; }
    public int CharacterLimit { get; set; } = 5_000;
    public string CountryHint { get; set; } = "us";

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Endpoint))
            throw new InvalidOperationException($"{nameof(Endpoint)} is required");
        
        if (string.IsNullOrWhiteSpace(ApiKey))
            throw new InvalidOperationException($"{nameof(ApiKey)} is required");
        
        if (string.IsNullOrWhiteSpace(CountryHint))
            throw new InvalidOperationException($"{nameof(CountryHint)} is required");
        
        if (CharacterLimit < 1)
            throw new InvalidOperationException($"{nameof(CharacterLimit)} must be greater than 0");
    }
}
using JetBrains.Annotations;

namespace CX.Engine.DocExtractors.Text;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class Gpt4VisionExtractorOptions
{
    public string ChatAgent { get; set; } = null!;
    public string SystemPrompt { get; set; } = null!;
    public string Question { get; set; } = null!;
    public string JsonStore { get; set; } = null!;
    public bool UseCache { get; set; } = true;

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(ChatAgent))
            throw new InvalidOperationException($"{nameof(Gpt4VisionExtractorOptions)}.{nameof(ChatAgent)} is required");
        
        if (string.IsNullOrWhiteSpace(SystemPrompt))
            throw new InvalidOperationException($"{nameof(Gpt4VisionExtractorOptions)}.{nameof(SystemPrompt)} is required");
        
        if (string.IsNullOrWhiteSpace(Question))
            throw new InvalidOperationException($"{nameof(Gpt4VisionExtractorOptions)}.{nameof(Question)} is required");
        
        if (string.IsNullOrWhiteSpace(JsonStore))
            throw new InvalidOperationException($"Missing {nameof(Gpt4VisionExtractorOptions)}.{nameof(JsonStore)}");
    }
}
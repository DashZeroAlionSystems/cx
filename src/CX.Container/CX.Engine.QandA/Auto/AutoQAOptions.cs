using JetBrains.Annotations;

namespace CX.Engine.QAndA.Auto;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class AutoQAOptions
{
    public string ChatAgent { get; set; } = null!;
    public string QuestionPrompt { get; set; } = null!;
    public string MemoryArchive { get; set; } = null!;
    public string EvalPrompt { get; set; } = null!;
    public string Assistant { get; set; } = null!;

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(ChatAgent))
            throw new InvalidOperationException($"{nameof(AutoQAOptions)}.{nameof(ChatAgent)} cannot be null or whitespace");
        
        if (string.IsNullOrWhiteSpace(QuestionPrompt))
            throw new InvalidOperationException($"{nameof(AutoQAOptions)}.{nameof(QuestionPrompt)} cannot be null or whitespace");
        
        if (string.IsNullOrWhiteSpace(MemoryArchive))
            throw new InvalidOperationException($"{nameof(AutoQAOptions)}.{nameof(MemoryArchive)} cannot be null or whitespace");

        if (string.IsNullOrWhiteSpace(EvalPrompt))
            throw new InvalidOperationException($"{nameof(AutoQAOptions)}.{nameof(EvalPrompt)} cannot be null or whitespace");

        if (string.IsNullOrWhiteSpace(Assistant))
            throw new InvalidOperationException($"{nameof(AutoQAOptions)}.{nameof(Assistant)} cannot be null or whitespace");
    }
}
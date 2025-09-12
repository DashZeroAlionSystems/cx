public class AnythingToMarkdownOptions
{
    public string ScriptPath { get; set; } = null!;
    public string BinaryStore { get; set; } = null!;
    public string PythonProcess { get; set; } = null!;

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(ScriptPath))
            throw new ArgumentException($"{nameof(AnythingToMarkdownOptions)}.{nameof(ScriptPath)} is required");

        if (string.IsNullOrWhiteSpace(BinaryStore))
            throw new InvalidOperationException($"Missing {nameof(AnythingToMarkdownOptions)}.{nameof(BinaryStore)}");

        if (string.IsNullOrWhiteSpace(PythonProcess))
            throw new InvalidOperationException($"Missing {nameof(AnythingToMarkdownOptions)}.{nameof(PythonProcess)}");
    }
}
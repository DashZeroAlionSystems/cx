using JetBrains.Annotations;

namespace CX.Engine.DocExtractors.Text;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class PDFPlumberOptions
{
    public string ScriptPath { get; set; } = null!;
    public string BinaryStore { get; set; } = null!;
    public string PythonProcess { get; set; } = null!;

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(PythonProcess))
            throw new ArgumentException($"{nameof(PDFPlumberOptions)}.{nameof(PythonProcess)} is required");

        if (string.IsNullOrWhiteSpace(ScriptPath))
            throw new ArgumentException($"{nameof(PDFPlumberOptions)}.{nameof(ScriptPath)} is required");

        if (string.IsNullOrWhiteSpace(BinaryStore))
            throw new InvalidOperationException($"Missing {nameof(PDFPlumberOptions)}.{nameof(BinaryStore)}");
    }
}
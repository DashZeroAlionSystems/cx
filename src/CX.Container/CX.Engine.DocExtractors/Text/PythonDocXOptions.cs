using JetBrains.Annotations;

namespace CX.Engine.DocExtractors.Text;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class PythonDocXOptions
{
    public string ScriptPath { get; set; } = null!;
    public string BinaryStore { get; set; } = null!;
    public string PythonProcess { get; set; } = null!;

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(ScriptPath))
            throw new ArgumentException($"{nameof(PythonDocXOptions)}.{nameof(ScriptPath)} is required");
        
        if (string.IsNullOrWhiteSpace(BinaryStore))
            throw new InvalidOperationException($"Missing {nameof(PythonDocXOptions)}.{nameof(BinaryStore)}");
        
        if (string.IsNullOrWhiteSpace(PythonProcess))
            throw new InvalidOperationException($"Missing {nameof(PythonDocXOptions)}.{nameof(PythonProcess)}");

    }
}
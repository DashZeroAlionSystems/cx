using JetBrains.Annotations;

namespace CX.Engine.Common.Python;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class PythonProcessOptions
{
    public string PythonInterpreterPath { get; set; } = null!;
    public string WorkingDir { get; set; } = null!;

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(PythonInterpreterPath))
            throw new ArgumentException($"{nameof(PythonProcessOptions)}.{nameof(PythonInterpreterPath)} is required");

        if (string.IsNullOrWhiteSpace(WorkingDir))
            throw new ArgumentException($"{nameof(PythonProcessOptions)}.{nameof(WorkingDir)} is required");
    }
}
using System.ComponentModel;
using JetBrains.Annotations;

namespace CX.Engine.DocExtractors;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class DocXToPDFOptions
{
    [DefaultValue(true)]
    public bool Enabled { get; set; } = true;
    public string BinaryStore { get; set; } = null!;

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(BinaryStore))
            throw new InvalidOperationException($"Missing {nameof(DocXToPDFOptions)}.{nameof(BinaryStore)}");
    }
}
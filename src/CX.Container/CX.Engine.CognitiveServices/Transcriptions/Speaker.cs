using JetBrains.Annotations;

namespace CX.Engine.CognitiveServices.Transcriptions;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class Speaker
{
    public string Name { get; set; }
    public string Role { get; set; }

    public Speaker()
    {
    }
    
    public Speaker([NotNull] string name, string role)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Role = role ?? throw new ArgumentNullException(nameof(role));
    }

    public Speaker([NotNull] string name)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
    }
}
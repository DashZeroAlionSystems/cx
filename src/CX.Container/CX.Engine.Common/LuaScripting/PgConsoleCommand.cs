using JetBrains.Annotations;

namespace CX.Engine.Common;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class PgConsoleCommand
{
    public int Id { get; set; }
    public Guid? ServiceId { get; set; }
    public string Command { get; set; }
}
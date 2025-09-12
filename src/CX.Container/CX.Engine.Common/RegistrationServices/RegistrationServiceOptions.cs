using JetBrains.Annotations;

namespace CX.Engine.Common.RegistrationServices;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class RegistrationServiceOptions
{
    public string LuaCore { get; set; }
    public string[] StartupTasks { get; set; } = null!;
}
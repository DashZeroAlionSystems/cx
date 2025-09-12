using CX.Engine.Common;
using JetBrains.Annotations;

namespace CX.Engine.Assistants.Walter1;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public sealed class Walter1AssistantOptionsOverrides : AgentOverride
{
    public string[] AddArchives { get; set; }
    public string[] RemoveArchives { get; set; }
}
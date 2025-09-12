using JetBrains.Annotations;

namespace CX.Engine.Common.ACL;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class ACLServiceOptions
{
    public Dictionary<string, ACLKeyEntry> APIKeys { get; set; } = new();
}
using JetBrains.Annotations;

namespace CX.Engine.Common.ACL;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class ACLKeyEntry
{
    public string[] Allow { get; set; }
    public string[] Deny { get; set; }
}
using JetBrains.Annotations;

namespace CX.Engine.DemoConsole;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class MoonyApiServiceOptions
{
    public string Url { get; set; } = "http://localhost:2981";
    public List<MoonyApiChannel> Channels { get; set; } = [];
}
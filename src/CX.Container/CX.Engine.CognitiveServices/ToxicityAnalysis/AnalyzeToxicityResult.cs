using JetBrains.Annotations;

namespace CX.Engine.CognitiveServices.ToxicityAnalysis;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class AnalyzeToxicityResult
{
    public bool ContainsToxicContent { get; set; }
    public Dictionary<string, int> CategoryLevels { get; set; } = [];
}
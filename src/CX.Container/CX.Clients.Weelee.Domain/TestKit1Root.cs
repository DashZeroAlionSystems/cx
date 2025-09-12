using JetBrains.Annotations;

namespace CX.Clients.Weelee.Domain;

using System.Collections.Generic;
using System.Text.Json.Serialization;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class TestKit1Root
{
    [JsonPropertyName("cars")] public List<TestKit1Car> Cars { get; set; }
    
    public List<string> StockNos => Cars.Select(c => c.StockNo).ToList();
    public List<string> StockNosUnique => Cars.Select(c => c.StockNo).Distinct().ToList();

    public bool AllCarsSemantic => Cars.All(c => c.Semantic);

    [JsonPropertyName("filters")] public TestKit1Filters Filters { get; set; }
    
    [JsonPropertyName("intro")] public string Intro { get; set; }
    
    [JsonPropertyName("sort")] public TestKit1Sort Sort { get; set; }
}
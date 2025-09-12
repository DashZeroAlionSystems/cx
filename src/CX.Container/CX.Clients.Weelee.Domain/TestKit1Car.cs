using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace CX.Clients.Weelee.Domain;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class TestKit1Car
{
    [JsonPropertyName("semantic")]
    public bool Semantic { get; set; }

    [JsonPropertyName("similarity")]
    public double? Similarity { get; set; }

    [JsonPropertyName("stock_no")] public string StockNo { get; set; }
}
using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace CX.Clients.Weelee.Domain;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class TestKit1Sort
{
    [JsonPropertyName("cubiccapacity")]
    public string CubicCapacity { get; set; }

    [JsonPropertyName("doors")]
    public string Doors { get; set; }

    [JsonPropertyName("mileage")]
    public string Mileage { get; set; }

    [JsonPropertyName("price")]
    public string Price { get; set; }

    [JsonPropertyName("seats")]
    public string Seats { get; set; }

    [JsonPropertyName("year")]
    public string Year { get; set; }

    [JsonPropertyName("Reasoning")]
    public string Reasoning { get; set; }
}
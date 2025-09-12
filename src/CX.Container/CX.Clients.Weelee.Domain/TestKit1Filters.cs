using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace CX.Clients.Weelee.Domain;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class TestKit1Filters
{
    [JsonPropertyName("Reasoning")] public string Reasoning { get; set; }

    [JsonPropertyName("SearchDatabase")]
    public bool SearchDatabase { get; set; }

    [JsonPropertyName("BodyType")] public List<string> BodyType { get; set; }

    [JsonPropertyName("Colour")]
    public List<string> Colour { get; set; }
 
    [JsonPropertyName("CubicCapacityMin")]
    public int CubicCapacityMin { get; set; }

    [JsonPropertyName("CubicCapacityMax")]
    public int CubicCapacityMax { get; set; }

    [JsonPropertyName("DoorsMin")]
    public int DoorsMin { get; set; }

    [JsonPropertyName("DoorsMax")]
    public int DoorsMax { get; set; }

    [JsonPropertyName("FuelType")]
    public List<string> FuelType { get; set; }

    [JsonPropertyName("Make")]
    public List<string> Make { get; set; }

    [JsonPropertyName("MileageMin")]
    public int MileageMin { get; set; }

    [JsonPropertyName("MileageMax")]
    public int MileageMax { get; set; }

    [JsonPropertyName("PriceMin")]
    public int PriceMin { get; set; }

    [JsonPropertyName("PriceMax")]
    public int PriceMax { get; set; }

    [JsonPropertyName("SeatsMin")]
    public int SeatsMin { get; set; }

    [JsonPropertyName("SeatsMax")]
    public int SeatsMax { get; set; }

    [JsonPropertyName("Transmission")]
    public List<string> Transmission { get; set; }

    [JsonPropertyName("YearMin")]
    public int YearMin { get; set; }

    [JsonPropertyName("YearMax")]
    public int YearMax { get; set; }
}
using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace CX.Engine.Assistants;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class RankedChunk
{
    [JsonInclude] public int Rank;
    [JsonInclude] public string Content = null!;
    [JsonInclude] public double Similarity;

    public RankedChunk()
    {
    }

    public RankedChunk(string content, int rank, double similarity)
    {
        Rank = rank;
        Content = content;
        Similarity = similarity;
    }
}
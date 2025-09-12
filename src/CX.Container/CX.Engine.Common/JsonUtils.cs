using System.Text.Json;
using System.Text.Json.Nodes;

namespace CX.Engine.Common;

public static class JsonUtils
{
    public static bool GetTruthy(this JsonNode node)
    {
        if (node == null)
            return false;

        var kind = node.GetValueKind();

        if (kind == JsonValueKind.True)
            return true;

        if (kind == JsonValueKind.False)
            return false;

        if (kind == JsonValueKind.String)
        {
            var s = node.GetValue<string>();
            return !string.IsNullOrWhiteSpace(s);
        }

        if (kind == JsonValueKind.Number)
        {
            var n = node.GetValue<decimal>();
            return n != 0;
        }

        if (kind == JsonValueKind.Array)
            return ((JsonArray)node).Count > 0;

        if (kind == JsonValueKind.Object)
            return ((JsonObject)node).Count > 0;

        return false;
    }

    /// <summary>
    /// Splits an array of JsonObjects n-ways deterministically.
    /// </summary>
    /// <param name="source"></param>
    /// <param name="segments"></param>
    /// <param name="segmentLimit"></param>
    /// <returns></returns>
    public static List<JsonObject>[] DeterministicSplit(this List<JsonObject> source, int segments, int segmentLimit)
    {
        if (segments < 0)
            throw new ArgumentException($"{nameof(segments)} must be greater than or equal to 0");

        if (segments == 0)
            return [];

        if (segments == 1)
            return [ source.Take(segmentLimit).ToList() ];

        var res = new List<JsonObject>[segments];
        for (var i = 0; i < segments; i++)
            res[i] = new();

        foreach (var item in source)
        {
            //Distribute items based on their JsonNodeEquality hashes (which take into account all of their content)
            var bucketNo = Math.Abs(DeterministicJsonHash.DeterministicHashCode(item)) % segments;

            //If our target bucket is full, we place the item into another bucket
            //We increment bucketNos in a circular fashion to check all buckets
            //And we keep track of how many full buckets we encounter 
            var full = 0;
            for (var i = 0; i < res.Length; i++)
            {
                var segment = res[(bucketNo + i) % res.Length];

                if (segment.Count < segmentLimit)
                {
                    segment.Add(item);
                    break;
                }
                else
                {
                    full++;
                    continue;
                }
            }

            //If all buckets are full we are done
            if (full == res.Length)
                break;
        }

        return res;
    }
    
    
}
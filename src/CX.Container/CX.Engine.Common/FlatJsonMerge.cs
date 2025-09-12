using System.Text.Json;
using Json.More;

namespace CX.Engine.Common;

using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;

public static class FlatJsonMerge
{
    private static bool IsUnset(JsonNode node)
    {
        if (node == null)
            return true;

        switch (node.GetValueKind())
        {
            case JsonValueKind.String:
                return string.IsNullOrWhiteSpace(node.GetValue<string>());
            case JsonValueKind.Null:
            case JsonValueKind.Undefined:
                return true;
        }

        return false;
    }

    /// <summary>
    /// Merges two JSON properties, by only considering the properties at the root of the objects.
    /// Arrays are merged and deduplicated.
    /// Properties in both objects are converted into arrays and deduplicated.
    /// Empty or whitespace strings are considered equivalent to undefined properties.
    /// </summary>
    public static JsonObject Merge(JsonObject obj1, JsonObject obj2)
    {
        var result = new JsonObject();

        // Collect all property names from both objects
        var allPropertyNames = new HashSet<string>(obj1.Select(p => p.Key));
        allPropertyNames.UnionWith(obj2.Select(p => p.Key));

        foreach (var propName in allPropertyNames)
        {
            var token1 = obj1[propName];
            var token2 = obj2[propName];

            if (IsUnset(token1))
                token1 = null;

            if (IsUnset(token2))
                token2 = null;

            if (token1 == null && token2 != null)
            {
                // Only token2 exists
                result[propName] = token2.DeepClone(); 
            }
            else if (token1 != null && token2 == null)
            {
                // Only token1 exists
                result[propName] = token1.DeepClone();
            }
            else if (token1 != null)
            {
                // Both tokens exist
                result[propName] = MergeNodes(token1, token2);
            }
        }

        return result;
    }

    private static JsonNode MergeNodes(JsonNode node1, JsonNode node2)
    {
        if (node1 is JsonArray array1)
        {
            var mergedArray = new JsonArray();
            var hashes = new HashSet<JsonNode>(JsonNodeEqualityComparer.Instance);

            foreach (var item in array1)
            {
                if (IsUnset(item))
                    continue;
                
                if (hashes.Add(item))
                    mergedArray.Add(item.DeepClone());
            }

            if (node2 is JsonArray array2)
            {
                foreach (var item in array2)
                {
                    if (IsUnset(item))
                        continue;

                    if (hashes.Add(item))
                        mergedArray.Add(item.DeepClone());
                }
            }
            else
            {
                mergedArray.Add(node2.DeepClone());
            }

            return mergedArray;
        }
        else if (node2 is JsonArray array2)
        {
            var mergedArray = new JsonArray();
            var isDup = false;
            foreach (var item in array2)
                if (JsonNodeEqualityComparer.Instance.Equals(item, node1))
                    isDup = true;
                
            if (!isDup)
                mergedArray.Add(node1.DeepClone());

            foreach (var item in array2)
                mergedArray.Add(item.DeepClone());

            return mergedArray;
        }
        else
        {
            if (JsonNodeEqualityComparer.Instance.Equals(node1, node2))
                return node1.DeepClone();
            
            return new JsonArray([node1.DeepClone(), node2.DeepClone()]);
        }
    }
}
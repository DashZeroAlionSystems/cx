using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;

public static class JsonDuplicateKeyChecker
{
    /// <summary>
    /// Checks the provided JsonObject for duplicate keys that differ only in case.
    /// Throws an exception with the path to the duplicate key if found.
    /// </summary>
    /// <param name="jsonObject">The JsonObject to check.</param>
    public static void CheckForDuplicateKeys(JsonObject jsonObject)
    {
        if (jsonObject == null)
            throw new ArgumentNullException(nameof(jsonObject));

        CheckNode(jsonObject, "$");
    }

    private static void CheckNode(JsonNode node, string path)
    {
        // If the node is an object, iterate its properties.
        if (node is JsonObject jsonObj)
        {
            // Use a dictionary with a case-insensitive comparer to track keys.
            var seenKeys = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var kvp in jsonObj)
            {
                var key = kvp.Key;

                // Check if a key with the same case-insensitive value was already seen.
                if (seenKeys.TryGetValue(key, out var originalKey))
                {
                    // Throw if the key has different case.
                    if (!string.Equals(key, originalKey, StringComparison.Ordinal))
                    {
                        throw new InvalidOperationException(
                            $"Duplicate key with different casing found at path '{path}': '{originalKey}' and '{key}'");
                    }
                }
                else
                {
                    seenKeys[key] = key;
                }

                // Recursively check the node's value.
                var childPath = $"{path}.{key}";
                CheckNode(kvp.Value, childPath);
            }
        }
        // If the node is an array, iterate through all items.
        else if (node is JsonArray jsonArray)
        {
            var index = 0;
            foreach (var element in jsonArray)
            {
                var childPath = $"{path}[{index}]";
                CheckNode(element, childPath);
                index++;
            }
        }
        // For JsonValue nodes (or null) there's nothing to check.
    }
}

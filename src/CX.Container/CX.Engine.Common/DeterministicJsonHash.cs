using System.Text.Json.Nodes;

namespace CX.Engine.Common
{
    public static class DeterministicJsonHash
    {
        // Simple prime-based combiner. 
        // Alternatively, you could do a 64-bit FNV-1a or any other stable scheme.
        private static int CombineHash(int current, int next)
        {
            unchecked
            {
                // For example, classic "hash * 31 ^ next".
                return (current * 31) ^ next;
            }
        }

        /// <summary>
        /// Returns a deterministic hash code for a JsonObject and all of its nested content.
        /// This will be identical across different .NET processes (assuming the JSON structure is the same).
        /// </summary>
        public static int DeterministicHashCode(JsonObject obj)
        {
            // Start with some non-zero seed
            var hash = 17;

            // Sort properties by key to ensure a consistent traversal order
            foreach (var kvp in obj.OrderBy(k => k.Key, StringComparer.Ordinal))
            {
                // Combine hash of property name
                hash = CombineHash(hash, GetStringDeterministicHash(kvp.Key));

                // Combine hash of property value (could be a JsonValue/JsonArray/JsonObject)
                hash = CombineHash(hash, DeterministicHashCode(kvp.Value));
            }

            return hash;
        }

        /// <summary>
        /// Overload that handles any kind of JsonNode (object, array, value, or null).
        /// </summary>
        private static int DeterministicHashCode(JsonNode node)
        {
            if (node is null)
                return 5849 * 6907;

            return node switch
            {
                JsonValue jsonValue => DeterministicHashCode(jsonValue),
                JsonObject jsonObject => DeterministicHashCode(jsonObject),
                JsonArray jsonArray => DeterministicHashCode(jsonArray),
                _ => throw new NotSupportedException($"Unsupported JsonNode type: {node.GetType()}")
            };
        }

        /// <summary>
        /// Deterministic hash code for a JsonValue.
        /// </summary>
        private static int DeterministicHashCode(JsonValue value)
        {
            // The "primitive" inside the JsonValue
            var raw = value.ToPrimitive();

            // If it's a string, a bool, a numeric, or a Guid, handle accordingly
            // You can refine this logic if you have more special types to handle.
            return raw switch
            {
                null => 5849 * 6907,
                string s => GetStringDeterministicHash(s),
                bool b => b ? 1231 : 1237,   // for example
                int i => i.GetHashCode(),
                long l => (int) (l ^ (l >> 32)),
                float f => f.GetHashCode(), // these are stable in .NET
                double d => d.GetHashCode(),
                decimal m => m.GetHashCode(),
                Guid g => g.GetHashCode(),  // stable across runs for the same GUID,
                _ => throw new NotSupportedException($"Unsupported JsonValue type: {raw.GetType()}")
            };
        }

        /// <summary>
        /// Deterministic hash code for a JsonArray.
        /// </summary>
        private static int DeterministicHashCode(JsonArray array)
        {
            var hash = 19;
            foreach (var element in array)
            {
                hash = CombineHash(hash, DeterministicHashCode(element));
            }
            return hash;
        }

        /// <summary>
        /// Simple example of a stable string hash. By default, .NET's string.GetHashCode() 
        /// is not guaranteed stable across processes if string interning or randomization is enabled.
        /// 
        /// Here we do a straightforward "FNV-1a" to ensure we always get the same value in every run.
        /// </summary>
        private static int GetStringDeterministicHash(string text)
        {
            unchecked
            {
                const int fnvPrime = 16777619;
                const int fnvOffset = (int) 2166136261;
                
                var hash = fnvOffset;
                foreach (var c in text)
                {
                    hash ^= c;
                    hash *= fnvPrime;
                }
                return hash;
            }
        }
    }
}

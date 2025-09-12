using System.Text.Json.Nodes;
using Json.More;

namespace CX.Engine.Common.Json;

public class JsonNodeHashSet : HashSet<JsonNode>
{
    public JsonNodeHashSet() : base(JsonNodeEqualityComparer.Instance)
    {
    }
}
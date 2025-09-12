using System.Dynamic;
using System.Text.Json;

namespace CX.Engine.Common.Json;

public static class JsonDocumentConverter
{
    public static dynamic ToDynamic(this JsonDocument doc)
    {
        return ToDynamic(doc.RootElement);
    }

    private static dynamic ToDynamic(this JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                var expandoObject = new ExpandoObject() as IDictionary<string, object>;
                foreach (var property in element.EnumerateObject())
                {
                    expandoObject.Add(property.Name, ToDynamic(property.Value));
                }
                return expandoObject;

            case JsonValueKind.Array:
                var list = new List<dynamic>();
                foreach (var item in element.EnumerateArray())
                {
                    list.Add(ToDynamic(item));
                }
                return list;

            case JsonValueKind.String:
                return element.GetString();

            case JsonValueKind.Number:
                if (element.TryGetInt64(out var l))
                    return l;
                if (element.TryGetDouble(out var d))
                    return d;
                return element.GetDecimal();

            case JsonValueKind.True:
                return true;

            case JsonValueKind.False:
                return false;

            case JsonValueKind.Null:
                return null;

            default:
                return null;
        }
    }
}
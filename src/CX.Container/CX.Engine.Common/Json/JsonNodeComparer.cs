using System.Text.Json.Nodes;

namespace CX.Engine.Common.Json;

public class JsonNodeComparer : IComparer<JsonNode>
{
    public static JsonNodeComparer Instance = new();
    
    public int Compare(JsonNode x, JsonNode y)
    {
        if (ReferenceEquals(x, y))
            return 0;
        if (x is null)
            return -1;
        if (y is null)
            return 1;

        // Determine the type order
        var xTypeOrder = GetTypeOrder(x);
        var yTypeOrder = GetTypeOrder(y);

        if (xTypeOrder != yTypeOrder)
            return xTypeOrder.CompareTo(yTypeOrder);

        // Types are the same, compare accordingly
        switch (x)
        {
            case JsonValue xValue when y is JsonValue yValue:
                return CompareJsonValues(xValue, yValue);

            case JsonArray xArray when y is JsonArray yArray:
                return CompareJsonArrays(xArray, yArray);

            case JsonObject xObject when y is JsonObject yObject:
                return CompareJsonObjects(xObject, yObject);

            default:
                throw new InvalidOperationException("Unhandled JsonNode type.");
        }
    }

    private int GetTypeOrder(JsonNode node)
    {
        return node switch
        {
            JsonValue => 1,
            JsonArray => 2,
            JsonObject => 3,
            _ => int.MaxValue
        };
    }

    private int CompareJsonValues(JsonValue xValue, JsonValue yValue)
    {
        var xVal = xValue.ToPrimitive();
        var yVal = yValue.ToPrimitive();

        // Handle nulls
        if (xVal == null && yVal == null)
            return 0;
        if (xVal == null)
            return -1;
        if (yVal == null)
            return 1;

        // Specific handling for int
        if (xVal is int xInt && yVal is int yInt)
        {
            return xInt.CompareTo(yInt);
        }

        // Specific handling for long
        if (xVal is long xLong && yVal is long yLong)
        {
            return xLong.CompareTo(yLong);
        }

        // Handling int and long comparison
        if (xVal is int xi && yVal is long yl)
        {
            return ((long)xi).CompareTo(yl);
        }
        if (xVal is long xl && yVal is int yi)
        {
            return xl.CompareTo((long)yi);
        }

        // Specific handling for Guid
        if (xVal is Guid xGuid && yVal is Guid yGuid)
        {
            return xGuid.CompareTo(yGuid);
        }

        // If both are numeric types, compare as decimal
        if (IsNumericType(xVal) && IsNumericType(yVal))
        {
            var xNum = Convert.ToDecimal(xVal);
            var yNum = Convert.ToDecimal(yVal);
            return xNum.CompareTo(yNum);
        }

        // If both are strings, compare as strings
        if (xVal is string xStr && yVal is string yStr)
        {
            return string.Compare(xStr, yStr, StringComparison.Ordinal);
        }

        // If both are booleans, compare as booleans
        if (xVal is bool xBool && yVal is bool yBool)
        {
            return xBool.CompareTo(yBool);
        }

        // If types are different, define an order
        var xTypeOrder = GetPrimitiveTypeOrder(xVal);
        var yTypeOrder = GetPrimitiveTypeOrder(yVal);

        if (xTypeOrder != yTypeOrder)
            return xTypeOrder.CompareTo(yTypeOrder);

        // Fallback to string comparison
        var xString = xVal.ToString();
        var yString = yVal.ToString();
        return string.Compare(xString, yString, StringComparison.Ordinal);
    }

    private int GetPrimitiveTypeOrder(object value)
    {
        if (value is null)
            return 0;
        if (value is bool)
            return 1;
        if (value is int)
            return 2;
        if (value is long)
            return 3;
        if (IsNumericType(value))
            return 4;
        if (value is Guid)
            return 5;
        if (value is string)
            return 6;
        // Add other types if necessary
        return int.MaxValue;
    }

    private bool IsNumericType(object value)
    {
        return value is byte || value is sbyte ||
               value is short || value is ushort ||
               value is uint || value is ulong ||
               value is float || value is double ||
               value is decimal;
    }

    private int CompareJsonArrays(JsonArray xArray, JsonArray yArray)
    {
        var minCount = Math.Min(xArray.Count, yArray.Count);
        for (var i = 0; i < minCount; i++)
        {
            var cmp = Compare(xArray[i], yArray[i]);
            if (cmp != 0)
                return cmp;
        }
        return xArray.Count.CompareTo(yArray.Count);
    }

    private int CompareJsonObjects(JsonObject xObject, JsonObject yObject)
    {
        var xProperties = xObject.OrderBy(kvp => kvp.Key).ToList();
        var yProperties = yObject.OrderBy(kvp => kvp.Key).ToList();

        var minCount = Math.Min(xProperties.Count, yProperties.Count);
        for (var i = 0; i < minCount; i++)
        {
            var keyComparison = string.Compare(xProperties[i].Key, yProperties[i].Key, StringComparison.Ordinal);
            if (keyComparison != 0)
                return keyComparison;

            var valueComparison = Compare(xProperties[i].Value, yProperties[i].Value);
            if (valueComparison != 0)
                return valueComparison;
        }
        return xProperties.Count.CompareTo(yProperties.Count);
    }
}

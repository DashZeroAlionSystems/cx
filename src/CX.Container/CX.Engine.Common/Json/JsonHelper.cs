using System.Text.Json;
using System.Text.Json.Nodes;

namespace CX.Engine.Common.Json;

/// <summary>
/// Helper methods for System.Text.Json
/// </summary>
public static class JsonHelper
{
    private static Dictionary<string, object> ConvertJsonElementToDictionary(JsonElement element)
    {
        var dict = new Dictionary<string, object>();

        foreach (var property in element.EnumerateObject())
            dict[property.Name] = ConvertJsonElementToObject(property.Value);

        return dict;
    }

    public static void ReadOrThrow(this ref Utf8JsonReader jr)
    {
        if (!jr.Read())
            throw new JsonException("Unexpected end of JSON input");
    }

    public static void Read(this ref Utf8JsonReader jr, JsonTokenType tokenType)
    {
        jr.ReadOrThrow();

        if (jr.TokenType != tokenType)
            throw new JsonException($"Expected {tokenType} but got {jr.TokenType}");
    }

    public static JsonTokenType Read(this ref Utf8JsonReader jr, params JsonTokenType[] tokenType)
    {
        jr.ReadOrThrow();

        if (!tokenType.Contains(jr.TokenType))
            throw new JsonException($"Expected {string.Join(" or ", tokenType)} but got {jr.TokenType}");

        return jr.TokenType;
    }

    public delegate void Utf8JsonReaderDelegate(ref Utf8JsonReader jr);

    /// <summary>
    /// The current token should be <see cref="JsonTokenType.StartArray"/> to use this method.
    /// </summary>
    public static void ReadArrayContent(this ref Utf8JsonReader jr, bool advance, Utf8JsonReaderDelegate readElement)
    {
        if (advance)
            jr.Read();

        jr.TokenMustBe(JsonTokenType.StartArray);

        while (true)
        {
            jr.Read();
            if (jr.TokenType == JsonTokenType.EndArray)
                break;

            readElement(ref jr);
        }

        jr.TokenMustBe(JsonTokenType.EndArray);
    }

    public static void TokenMustBe(this ref Utf8JsonReader jr, JsonTokenType tokenType)
    {
        if (jr.TokenType != tokenType)
            throw new JsonException($"Expected {tokenType} but got {jr.TokenType}");
    }

    public static void TokenMustBe(this ref Utf8JsonReader jr, params JsonTokenType[] tokenTypes)
    {
        if (!tokenTypes.Contains(jr.TokenType))
            throw new JsonException($"Expected {string.Join(" or ", tokenTypes)} but got {jr.TokenType}");
    }

    public static string ReadStringValue(this ref Utf8JsonReader jr)
    {
        jr.Read();
        jr.TokenMustBe(JsonTokenType.String, JsonTokenType.Null);
        return jr.GetString();
    }

    public static void ReadArrayOfObject(this ref Utf8JsonReader jr, bool advance, Utf8JsonReaderDelegate readObject)
    {
        jr.ReadArrayContent(advance,
            (ref Utf8JsonReader jr) =>
            {
                jr.TokenMustBe(JsonTokenType.StartObject);
                readObject(ref jr);
                jr.TokenMustBe(JsonTokenType.EndObject);
            });
    }

    public delegate void Utf8JsonReaderDelegateProperty<in T>(ref Utf8JsonReader jr, T obj, string propertyName);

    public static void ReadObjectProperties<T>(this ref Utf8JsonReader jr, T obj, bool advance,
        Utf8JsonReaderDelegateProperty<T> readProperty)
    {
        if (advance)
            jr.Read();

        jr.TokenMustBe(JsonTokenType.StartObject);

        while (jr.Read())
        {
            if (jr.TokenType != JsonTokenType.PropertyName)
                break;

            var propertyName = jr.GetString();

            if (propertyName == null)
                throw new JsonException("Property name is null");

            readProperty(ref jr, obj, propertyName);
        }

        jr.TokenMustBe(JsonTokenType.EndObject);
    }

    public static void SkipPropertyValue(this ref Utf8JsonReader jr)
    {
        jr.TokenMustBe(JsonTokenType.PropertyName);
        jr.Read();
        jr.Skip();
    }

    public static long ReadInt64Value(this ref Utf8JsonReader jr)
    {
        jr.Read();
        jr.TokenMustBe(JsonTokenType.Number);
        return jr.GetInt64();
    }

    public static int ReadInt32Value(this ref Utf8JsonReader jr)
    {
        jr.Read();
        jr.TokenMustBe(JsonTokenType.Number);
        return jr.GetInt32();
    }


    /// <summary>
    /// Returns a native object graph with Dictionary&lt;string, object&gt; for objects.
    /// </summary>
    public static object ConvertJsonElementToObject(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                return ConvertJsonElementToDictionary(element);

            case JsonValueKind.Array:
                var list = new List<object>();
                foreach (var item in element.EnumerateArray())
                {
                    list.Add(ConvertJsonElementToObject(item));
                }

                return list;

            case JsonValueKind.String:
                return element.GetString();

            case JsonValueKind.Number:
                if (element.TryGetInt32(out var intValue))
                    return intValue;
                if (element.TryGetInt64(out var longValue))
                    return longValue;
                if (element.TryGetDouble(out var doubleValue))
                    return doubleValue;
                return element.GetDecimal();

            case JsonValueKind.True:
                return true;

            case JsonValueKind.False:
                return false;

            case JsonValueKind.Null:
                return null;

            default:
                return element.GetRawText();
        }
    }

    public static void WriteObject<T>(this Utf8JsonWriter jw, string propertyName, T o)
    {
        jw.WritePropertyName(propertyName);
        WriteObjectValue(jw, o);
    }

    public static void WriteObjectValue<T>(this Utf8JsonWriter jw, T o)
    {
        if (o == null)
        {
            jw.WriteNullValue();
            return;
        }

        if (o is Array array && o is not string)
        {
            jw.WriteStartArray();
            foreach (var v in array)
                WriteObjectValue(jw, v);
            jw.WriteEndArray();
            return;
        }

        if (o is ISerializeJson sj)
            sj.Serialize(jw);
        else
            JsonSerializer.Serialize(jw, o, MiscHelpers.JsonSerializerOptionsHuman);
    }

    /// <summary>
    /// Removes the specified JSON nodes from the nearest array.
    /// </summary>
    /// <param name="toRemove">An enumerable collection of <see cref="JsonNode"/> objects to be removed from their respective nearest arrays.</param>
    public static void RemoveNodesFromNearestArrays<TEnum>(this TEnum toRemove) where TEnum: IEnumerable<JsonNode> 
    {
        foreach (var tr in toRemove)
            tr.RemoveNodeFromNearestArray();
    }

    public static (JsonNode parent, JsonArray arr) GetParentInNearestArray(this JsonNode tr)
    {
        if (tr == null)
            throw new ArgumentNullException(nameof(tr));
        
        var obj = tr;
        var parent = obj.Parent;

        while (parent != null)
        {
            if (parent is JsonArray arr)
                return (obj, arr);

            if (parent is JsonObject o)
            {
                obj = o;
                parent = obj.Parent;
            }
        }

        return (null, null);
    }

    /// <summary>
    /// Removes the specified JSON nodes from the nearest array.
    /// </summary>
    /// <param name="node">The <see cref="JsonNode"/> to be removed from their nearest array.</param>
    public static void RemoveNodeFromNearestArray(this JsonNode node)
    {
        var (obj, arr) = GetParentInNearestArray(node);
        
        if (obj == null)
            throw new InvalidOperationException("The specified node is not in an array.");
        
        arr.Remove(obj);
    }

    public static void InlineArray(this JsonNode node)
    {
        if (node is JsonArray)
        {
            var parent = node.Parent;

            if (parent is not JsonArray ja)
                throw new InvalidOperationException("The parent of the specified node is not an array.");
            
            var index = ja.IndexOf(node);
            ja.RemoveAt(index);
            ja.InsertRange(index, ja);
        }
    }
    
    public static void InsertRange(this JsonArray ja, int index, IEnumerable<JsonNode> nodes)
    {
        foreach (var n in nodes)
            ja.Insert(index++, n);
    }

    /// <summary>
    /// Converts a JsonNode to a JsonElement.
    /// </summary>
    /// <param name="node">The JsonNode to convert.</param>
    /// <returns>A JsonElement representing the same JSON data.</returns>
    public static JsonElement ToJsonElement(this JsonNode node)
    {
        if (node == null)
            throw new ArgumentNullException(nameof(node));

        // Serialize the JsonNode to a JSON string
        var jsonString = node.ToJsonString();

        // Parse the JSON string into a JsonDocument
        var document = JsonDocument.Parse(jsonString);
        return document.RootElement;
    }}
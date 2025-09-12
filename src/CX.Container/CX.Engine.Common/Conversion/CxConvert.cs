using HtmlAgilityPack;
using Microsoft.Scripting.Runtime;

namespace CX.Engine.Common.Conversion;

public static class CxConvert
{
    public static async Task<T> ToAsync<T>(object input) => (T)(await ToAsync(typeof(T), input));

    /// <remarks>InnerText from HtmlNodes are used when casting from an HtmlNode to a string.</remarks>
    public static async Task<object> ToAsync(Type targetType, object src)
    {
        if (targetType == null)
            throw new ArgumentNullException(nameof(targetType));
        
        try
        {
            var sourceType = src?.GetType();
            
            if (sourceType == targetType)
                return src;

            if (targetType == typeof(object))
                return src;
            
            if (src.IsAwaitable())
            {
                src = await MiscHelpers.AwaitAnyAsync(src);
                sourceType = src?.GetType();
            }

            if (sourceType == targetType)
                return src;
            
            if (targetType.IsAssignableFrom(sourceType))
                return src;

            {//string inputs
                if (src is string s)
                {
                    if (targetType == typeof(string))
                        return s;
                    if (targetType == typeof(int))
                        return int.Parse(s);
                    if (targetType == typeof(int?))
                        return string.IsNullOrWhiteSpace(s) ? null : (int?)int.Parse(s);
                    if (targetType == typeof(long))
                        return long.Parse(s);
                    if (targetType == typeof(long?))
                        return string.IsNullOrWhiteSpace(s) ? null : (long?)long.Parse(s);
                    if (targetType == typeof(float))
                        return float.Parse(s);
                    if (targetType == typeof(float?))
                        return string.IsNullOrWhiteSpace(s) ? null : (float?)float.Parse(s);
                    if (targetType == typeof(double))
                        return double.Parse(s);
                    if (targetType == typeof(double?))
                        return string.IsNullOrWhiteSpace(s) ? null : (double?)double.Parse(s);
                    if (targetType == typeof(decimal))
                        return decimal.Parse(s);
                    if (targetType == typeof(decimal?))
                        return string.IsNullOrWhiteSpace(s) ? null : (decimal?)decimal.Parse(s);
                    if (targetType == typeof(bool))
                        return bool.Parse(s);
                    if (targetType == typeof(bool?))
                        return string.IsNullOrWhiteSpace(s) ? null : (bool?)bool.Parse(s);
                    if (targetType == typeof(HtmlNode))
                        return HtmlNode.CreateNode(s);
                }
            }

            if (targetType == typeof(string))
                if (src is HtmlNode node)
                    return node.InnerText;
                else
                    return src?.ToString();

            return Cast.Explicit(src, targetType);
        }
        catch (Exception ex)
        {
            throw new CxConvertException(ex);
        }
    }
    
    /// <summary>
    /// Converts a comma seperated string into a List&lt;int&gt;.
    /// Not tolerant to invalid input.
    /// Tolerant to whitespace.
    /// Tolerant to extra commas.
    /// </summary>
    /// <param name="s">The string to convert.</param>
    /// <returns>The list with all numbers that were in the string, in order.</returns>
    public static List<int> ToIntList(string s)
    {
        // Check if the string is null, if so return null
        if (s == null)
            return null;

        // Initialize a new list for the result
        var res = new List<int>();

        // Split the string by comma
        var split = s.Split(',');
        
        // If there are no elements after the split, return an empty list
        if (split.Length == 0)
            return [];

        // Loop over the split items
        foreach (var item in split)
        {
            // If the item is only whitespace or empty, skip it
            if (string.IsNullOrWhiteSpace(item))
                continue;

            // Convert the item to an integer, remove leading or trailing whitespace and add it to the result list
            res.Add(int.Parse(item.Trim()));
        }

        // Return the result list
        return res;
    }
}
using System.Text;

namespace CX.Container.Server.Extensions.Application;

public static class StringExtensions
{
    /// <summary>
    /// Checks if a string has no value.
    /// </summary>
    public static bool IsNullOrWhitespace(this string value)
    {
        return string.IsNullOrWhiteSpace(value);
    }

    /// <summary>
    /// Checks if a string has a value.
    /// </summary>
    public static bool IsNotNullOrWhiteSpace(this string value)
    {
        return !string.IsNullOrWhiteSpace(value);
    }
    
    /// <summary>
    /// Returns the first non-null or non-empty string from the given values.
    /// </summary>
    public static string Coalesce(this string value, params string[] values)
    {
        if (value.IsNotNullOrWhiteSpace())
        {
            return value;
        }

        foreach (var val in values)
        {
            if (val.IsNotNullOrWhiteSpace())
            {
                return val;
            }
        }

        return string.Empty;
    }
    
    /// <summary>
    /// Returns null if the string is empty or whitespace.
    /// </summary>
    public static string EmptyToNull(this string value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }
    
    public static string Truncate(this string input, int maxLength)
    {
        if (string.IsNullOrEmpty(input)) return input;
        
        var trimmedInput = input.Trim();

        return
            trimmedInput.Length > maxLength
                ? $"{trimmedInput.Substring(0, maxLength - 3)}..."
                : trimmedInput;
    }
    
    public static string Base64UrlDecode(this ReadOnlySpan<char> input)
    {
        // Allocate a buffer on the stack or heap depending on the length
        var output = input.Length <= 128 ? stackalloc char[input.Length] : new char[input.Length];

        // Copy the ReadOnlySpan to the Span
        input.CopyTo(output);

        // In-place character replacement
        for (int i = 0; i < output.Length; i++)
        {
            if (output[i] == '-') output[i] = '+';
            else if (output[i] == '_') output[i] = '/';
        }

        // Adjust padding
        int mod4 = output.Length % 4;
        string outputStr = mod4 switch
        {
            2 => new string(output) + "==",
            3 => new string(output) + "=",
            _ => new string(output)
        };

        // Check for invalid base64url string
        if (mod4 == 1)
        {
            throw new ArgumentException("Illegal base64url string!", nameof(input));
        }

        // Decode and convert to string
        byte[] converted = Convert.FromBase64String(outputStr);
        return Encoding.UTF8.GetString(converted);
    }
}
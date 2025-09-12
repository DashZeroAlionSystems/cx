using System.Text;

namespace CX.Engine.Common;

public static class Utf8Utils
{
    // Remove null characters from the string.
    private static string RemoveNulls(string input)
    {
        return input.Replace("\0", string.Empty);
    }

    // Validate and ensure the string is UTF-8 encoded.
    private static string Reencode(string input)
    {
        // Encode the string to UTF-8 bytes
        var utf8Bytes = Encoding.UTF8.GetBytes(input);

        // Decode the bytes back to a string
        var validatedString = Encoding.UTF8.GetString(utf8Bytes);

        return validatedString;
    }

    /// <summary>
    /// Removes nulls and re-encodes.
    /// </summary>
    public static string Sanitize(string input)
    {
        // Remove null characters
        var sanitizedText = RemoveNulls(input);

        // Validate UTF-8 encoding
        sanitizedText = Reencode(sanitizedText);

        return sanitizedText;
    }
}
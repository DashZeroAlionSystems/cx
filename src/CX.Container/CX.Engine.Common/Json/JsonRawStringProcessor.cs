using System.Text.RegularExpressions;

namespace CX.Engine.Common.Json;

public static class JsonRawStringProcessor
{
 /// <summary>
    /// Processes a JSON‑like string that uses C#‑style triple‑quoted raw strings,
    /// dedenting and replacing internal newlines with the literal "\r\n" escape sequence,
    /// so the result is valid JSON.
    /// </summary>
    /// <param name="input">The input JSON‑like text.</param>
    /// <returns>A string with triple‑quoted raw string literals replaced by valid JSON string values.</returns>
    public static string NormalizeTripleQuotedStrings(string input)
    {
        if (input == null)
            return null;
        
        if (input.Contains("\"\"\"") == false)
            return input;
        
        // Pattern to match a triple‑quoted string.
        // Using a raw string literal for clarity. The pattern matches:
        //   - Three double quotes
        //   - (Capture) any characters (including newlines) non‑greedily
        //   - Three double quotes
        var tripleQuotePattern = 
                                 "\"\"\"(.*?)\"\"\"";

        return Regex.Replace(input, tripleQuotePattern, match =>
        {
            // Get the raw content between the triple quotes.
            var content = match.Groups[1].Value;

            // Split the content into lines (handling CR, LF, or CRLF).
            var lines = content.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None)
                               .ToList();

            // According to C# raw string literal rules, if the first or last lines are entirely whitespace,
            // they are not considered part of the content.
            while (lines.Count > 0 && string.IsNullOrWhiteSpace(lines.First()))
            {
                lines.RemoveAt(0);
            }
            while (lines.Count > 0 && string.IsNullOrWhiteSpace(lines.Last()))
            {
                lines.RemoveAt(lines.Count - 1);
            }

            // Compute the minimum indent on non‑empty lines.
            var commonIndent = lines
                .Where(l => l.Trim().Length > 0)
                .Select(l => l.TakeWhile(Char.IsWhiteSpace).Count())
                .DefaultIfEmpty(0)
                .Min();

            // Remove the common indent.
            var dedented = lines.Select(line =>
                line.Length >= commonIndent ? line.Substring(commonIndent) : line
            );

            // Replace the newline characters with the literal escape sequence "\r\n".
            // (You could also choose to join with a single space if you want to collapse all newlines.)
            var normalized = string.Join("\\r\\n", dedented);
            
            //escape all double quotes
            normalized = normalized.Replace("\"", "\\\"");
            
            //escape all tab characters
            normalized = normalized.Replace("\t", "\\t");
            
            // Return the normalized string wrapped in standard double quotes.
            return $"\"{normalized}\"";
        }, RegexOptions.Singleline);
    }
}

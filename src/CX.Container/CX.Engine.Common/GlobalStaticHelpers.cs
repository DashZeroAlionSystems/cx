using System.Text;

namespace CX.Engine.Common;

/// <summary>
/// Helper functions that are designed to be in the global using context.
/// </summary>
public static class GlobalStaticHelpers
{
    /// <summary>
    /// Escapes and quotes each element in a params array of arguments.
    /// </summary>
    /// <param name="args">Array of arguments to be escaped and quoted.</param>
    /// <returns>A single string containing all arguments, escaped and quoted.</returns>
    public static string PyEscapeAndQuoteArgs(params string[] args)
    {
        var sb = new StringBuilder();
        foreach (var arg in args)
        {
            // Escape backslashes and double quotes in the argument
            var escaped = arg.Replace("\\", @"\\").Replace("\"", "\\\"");

            // Surround the argument with double quotes and append to the result
            sb.Append($"\"{escaped}\" ");

            // This adds a space after each argument. If this is not desirable (e.g., for the last argument),
            // you could handle this conditionally based on the position in the array.
        }

        // Trim the trailing space and return the result
        return sb.ToString().TrimEnd();
    }

    /// <summary>
    /// Tries to delete a file.  Ignores any exceptions encountered.
    /// </summary>
    /// <param name="filePath">The file to try and delete</param>
    /// <returns>True if the file no longer exists, or did not exist.</returns>
    public static bool TryDeleteFile(string filePath)
    {
        try
        {
            if (File.Exists(filePath))
                File.Delete(filePath);
            
            return true;
        }
        catch
        {
            //ignore all exceptions
            return false;
        }
    }
    
    public static async Task CreateFileFromStreamAsync(string filePath, Stream stream)
    {
        await using var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
        stream.Position = 0;
        await stream.CopyToAsync(fs);
    }
}
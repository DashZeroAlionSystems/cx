using System.Text.RegularExpressions;

namespace CX.Engine.Common;

public static class MarkdownLinkExtractor
{
    public static List<(string linkText, string Url, string full)> ExtractLinks(string input)
    {
        var links = new List<(string linkText, string url, string full)>();
        var pattern = @"\[([^\]]+)\]\(([^)]+)\)";

        foreach (Match match in Regex.Matches(input, pattern))
        {
            var linkText = match.Groups[1].Value;
            var url = match.Groups[2].Value;

            // Add the entire match to the list
            links.Add((linkText, url, match.Value));
        }

        return links;
    }
}
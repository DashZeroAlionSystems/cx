namespace CX.Engine.Common;

public static class MarkdownUtils
{
/// <summary>
    /// Converts a list of Markdown table lines into a MarkdownTable object,
    /// supporting multi-line headers by merging consecutive header rows before the alignment row.
    /// </summary>
    /// <param name="markdownLines">List of strings representing the Markdown table lines.</param>
    /// <returns>A MarkdownTable object with Headers and Rows populated.</returns>
    /// <exception cref="ArgumentException">Thrown when the input format is invalid.</exception>
    public static MarkdownTable ConvertMarkdownTable(List<string> markdownLines)
    {
        if (markdownLines == null || markdownLines.Count == 0)
            throw new ArgumentException("Markdown table input is empty.");

        // Remove any empty lines and trim whitespace
        var lines = markdownLines
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(line => line.Trim())
            .ToList();

        if (lines.Count < 2)
            throw new ArgumentException("Invalid Markdown table format. At least header and alignment rows are required.");

        // Identify the alignment row index
        var alignmentRowIndex = -1;
        for (var i = 0; i < lines.Count; i++)
        {
            if (IsAlignmentRow(lines[i]))
            {
                alignmentRowIndex = i;
                break;
            }
        }

        if (alignmentRowIndex == -1)
            throw new ArgumentException("The Markdown table must contain an alignment row (e.g., |---|---|).");

        if (alignmentRowIndex == 0)
            throw new ArgumentException("Alignment row cannot be the first line. There must be at least one header line before it.");

        // Extract header lines (all lines before the alignment row)
        var headerLines = lines.Take(alignmentRowIndex).ToList();

        // Parse each header line into cells
        var parsedHeaderLines = headerLines
            .Select(ParseMarkdownRow)
            .ToList();

        // Ensure all header lines have the same number of columns
        var columnCount = parsedHeaderLines[0].Count;
        foreach (var headerLine in parsedHeaderLines)
        {
            if (headerLine.Count != columnCount)
                throw new ArgumentException("All header lines must have the same number of columns.");
        }

        // Merge header lines by concatenating corresponding cells with spaces
        var headers = new List<string>();
        for (var col = 0; col < columnCount; col++)
        {
            var mergedHeader = string.Join(" ", parsedHeaderLines.Select(line => line[col]));
            headers.Add(mergedHeader.Trim());
        }

        // Parse content rows (all lines after the alignment row)
        var rows = new List<List<string>>();
        for (var i = alignmentRowIndex + 1; i < lines.Count; i++)
        {
            var row = ParseMarkdownRow(lines[i]);

            // Normalize the row to have the same number of columns as headers
            if (row.Count < columnCount)
            {
                // Pad with empty strings if there are fewer cells
                row.AddRange(Enumerable.Repeat(string.Empty, columnCount - row.Count));
            }
            else if (row.Count > columnCount)
            {
                // Trim excess cells if there are more
                row = row.Take(columnCount).ToList();
            }

            // Only add non-empty rows
            if (row.Any(cell => !string.IsNullOrWhiteSpace(cell)))
            {
                rows.Add(row);
            }
        }

        return new MarkdownTable
        {
            Headers = headers,
            Rows = rows
        };
    }
    
    public static List<string> ParseMarkdownRow(string line)
    {
        // Remove leading and trailing pipes and whitespace
        line = line.Trim().Trim('|').Trim();

        // Split by pipe and trim each cell
        var cells = line.Split('|')
            .Select(cell => cell.Trim())
            .ToList();

        return cells;
    }
    
    public static bool IsAlignmentRow(string line)
    {
        var trimmedLine = line.Trim();
        return trimmedLine.All(c => c == ':' || c == '-' || c == '|' || char.IsWhiteSpace(c)) && trimmedLine.Contains('-');
    }
}
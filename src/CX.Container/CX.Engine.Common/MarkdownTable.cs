using System.Text;
using System.Text.Json.Nodes;

namespace CX.Engine.Common;

public class MarkdownTable
{
    public List<string> Headers;
    public List<List<string>> Rows;

    public double[] ComputeColumnWidths()
    {
        var columnWidths = new double[Headers.Count];
        for (var i = 0; i < Headers.Count; i++)
            columnWidths[i] = Headers[i].Length;

        foreach (var row in Rows)
        {
            for (var i = 0; i < row.Count; i++)
                if (row[i].Length > columnWidths[i])
                    columnWidths[i] = row[i].Length;
        }

        return columnWidths;
    }
    
    public bool ColumnHasNonWhitespaceContent(int columnIndex) => Rows.Any(row => !string.IsNullOrWhiteSpace(row[columnIndex]));
    
    public void RemoveColumn(int columnIndex)
    {
        Headers.RemoveAt(columnIndex);
        foreach (var row in Rows)
            row.RemoveAt(columnIndex);
    }

    public void RemoveWhitespaceContentColumns()
    {
        for (var i = Headers.Count - 1; i >= 0; i--)
            if (!ColumnHasNonWhitespaceContent(i))
                RemoveColumn(i);
    }

    public void Pivot()
    {
        // Combine Headers and Rows into a single grid
        var grid = new List<List<string>>();
        grid.Add(new List<string>(Headers));

        foreach (var row in Rows)
        {
            // Ensure each row has the same number of columns as headers
            var paddedRow = new List<string>(row);
            while (paddedRow.Count < Headers.Count)
                paddedRow.Add(string.Empty);
            grid.Add(paddedRow);
        }

        // Determine the maximum number of columns in the grid
        var maxColumns = grid.Max(r => r.Count);

        // Initialize the transposed grid
        var transposedGrid = new List<List<string>>();

        for (var col = 0; col < maxColumns; col++)
        {
            var newRow = new List<string>();
            foreach (var row in grid)
            {
                if (col < row.Count)
                    newRow.Add(row[col]);
                else
                    newRow.Add(string.Empty);
            }
            transposedGrid.Add(newRow);
        }

        if (transposedGrid.Count == 0)
            return; // Nothing to pivot

        // The first row of the transposed grid becomes new Headers
        var newHeaders = transposedGrid[0];
        // The remaining rows become new Rows
        var newRows = transposedGrid.Skip(1).ToList();

        // Assign the new Headers and Rows to the table
        Headers = newHeaders;
        Rows = newRows;
    }
    
    public override string ToString()
    {
        var sb = new StringBuilder();
        
        var columnWidths = ComputeColumnWidths();
        
        // Header
        sb.Append("| ");
        for (var i = 0; i < Headers.Count; i++)
        {
            sb.Append(Headers[i].PadRight((int)columnWidths[i]));
            sb.Append(" | ");
        }
        sb.AppendLine();
        
        // Separator
        sb.Append("| ");
        for (var i = 0; i < Headers.Count; i++)
        {
            sb.Append(new string('-', (int)columnWidths[i]));
            sb.Append(" | ");
        }
        sb.AppendLine();
        
        // Rows
        foreach (var row in Rows)
        {
            sb.Append("| ");
            for (var i = 0; i < row.Count; i++)
            {
                sb.Append(row[i].PadRight((int)columnWidths[i]));
                sb.Append(" | ");
            }
            sb.AppendLine();
        }
        
        return sb.ToString();
    }

    public JsonArray ToJsonArray()
    {
        var arr = new JsonArray();
        foreach (var row in Rows)
        {
            var obj = new JsonObject();
            //deduplicate headers
            var usedHeaders = new HashSet<string>();
            for (var i = 0; i < Headers.Count; i++)
            {
                var header = Headers[i] ?? "";
                if (usedHeaders.Contains(header))
                    header += $"_{i}";
                usedHeaders.Add(header);
                obj[header] = row[i];
            }

            arr.Add(obj);
        }
        
        return arr;
    }

    public MarkdownTable Clone()
    {
        return new()
        {
            Headers = [..Headers],
            Rows = Rows.Select(row => new List<string>(row)).ToList()
        };
    }
}
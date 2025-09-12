using System.Data.Common;
using SmartFormat;

namespace CX.Engine.Assistants.QueryAssistants;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

public static class SqlMarkdownConverter
{
    private static string FormatDouble(double value, string format)
    {
        if(format == null)
            return value.ToString("F2", CultureInfo.InvariantCulture);
        
        return Smart.Format(format, value);
    }
    
    public static Task<string> ConvertToMarkdownAsync(List<dynamic> input, int? maxRows = null, string noRowMessage = null, Dictionary<string, string> formatters = null)
    {
        if (input == null)
            throw new ArgumentNullException(nameof(input));
        if (input.Count == 0)
            return Task.FromResult(string.IsNullOrWhiteSpace(noRowMessage) ? string.Empty : noRowMessage);
        
        var rows = new List<Dictionary<string, object>>(input.Select(x => (x as IDictionary<string, object>)!.ToDictionary(y => y.Key, y => y.Value)));
        
        var headers = rows[0].Keys.ToArray();
        var fieldCount = headers.Length;
        var maxWidths = new int[fieldCount];

        for (int i = 0; i < fieldCount; i++)
        {
            maxWidths[i] = headers[i].Length;
        }

        // Prepare processed rows as string arrays.
        var processedRows = new List<string[]>();
        var moreRows = false;

        var rowCount = 0;
        foreach (var row in rows)
        {
            rowCount++;
            if (maxRows.HasValue && rowCount > maxRows.Value)
            {
                moreRows = true;
                break;
            }

            var cells = new string[fieldCount];
            for (int i = 0; i < fieldCount; i++)
            {
                var key = headers[i];
                var value = row[key];
                string formatter = null;
                formatters?.TryGetValue(key, out formatter);

                string cellValue;
                if (value == null)
                {
                    cellValue = "";
                }
                else if (value is double d)
                {
                    cellValue = FormatDouble(d, formatter);
                }
                else if (value is float f)
                {
                    cellValue = FormatDouble(f, formatter);
                }
                else
                {
                    cellValue = value.ToString();
                }
                
                
                cells[i] = cellValue;
                if (cellValue.Length > maxWidths[i])
                {
                    maxWidths[i] = cellValue.Length;
                }
            }

            processedRows.Add(cells);
        }

        // Build Markdown
        var sb = new StringBuilder();

        // Header
        for (int i = 0; i < fieldCount; i++)
        {
            sb.Append("| ").Append(headers[i].PadRight(maxWidths[i])).Append(" ");
        }
        sb.AppendLine("|");

        // Separator
        for (int i = 0; i < fieldCount; i++)
        {
            sb.Append("| ").Append(new string('-', maxWidths[i])).Append(" ");
        }
        sb.AppendLine("|");

        // Data rows
        foreach (var row in processedRows)
        {
            for (int i = 0; i < fieldCount; i++)
            {
                sb.Append("| ").Append(row[i].PadRight(maxWidths[i])).Append(" ");
            }
            sb.AppendLine("|");
        }

        if (moreRows)
            sb.AppendLine("... and more rows");

        return Task.FromResult(sb.ToString());
    }
    
    /// <summary>
    /// Converts the content of an open SqlDataReader into a Markdown table string.
    /// The table will be aligned so that each column is padded to the maximum width,
    /// and any float or double values will be formatted with two decimal digits.
    /// </summary>
    /// <param name="reader">An open SqlDataReader with the data.</param>
    /// <param name="maxRows">The maximum number of rows to read from the reader. If null, all rows are read.</param>
    /// <returns>A string representing the data in Markdown table format.</returns>
    public static async Task<string> ConvertToMarkdownAsync(DbDataReader reader, int? maxRows = null)
    {
        if (reader == null)
            throw new ArgumentNullException(nameof(reader));
        if (reader.FieldCount == 0)
            return string.Empty;

        var fieldCount = reader.FieldCount;

        // Read header names.
        var headers = new string[fieldCount];
        var maxWidths = new int[fieldCount];
        for (var i = 0; i < fieldCount; i++)
        {
            headers[i] = reader.GetName(i) ?? "";
            maxWidths[i] = headers[i].Length;
        }

        // List to store each row as an array of strings.
        var rows = new List<string[]>();

        var rowCount = 0;
        // Read all rows from the reader.
        while (await reader.ReadAsync())
        {
            rowCount++;
            
            if (maxRows.HasValue && rowCount > maxRows.Value)
                break;
            
            var cells = new string[fieldCount];
            for (var i = 0; i < fieldCount; i++)
            {
                string cellValue;
                if (reader.IsDBNull(i))
                {
                    cellValue = "";
                }
                else
                {
                    // Check the type and format floats and doubles.
                    var fieldType = reader.GetFieldType(i);
                    if (fieldType == typeof(double))
                    {
                        var d = reader.GetDouble(i);
                        cellValue = d.ToString("F2", CultureInfo.InvariantCulture);
                    }
                    else if (fieldType == typeof(float))
                    {
                        var f = reader.GetFloat(i);
                        cellValue = f.ToString("F2", CultureInfo.InvariantCulture);
                    }
                    else
                    {
                        cellValue = reader.GetValue(i).ToString();
                    }
                }

                cells[i] = cellValue;
                // Update maximum width if needed.
                if (cellValue!.Length > maxWidths[i])
                {
                    maxWidths[i] = cellValue.Length;
                }
            }
            rows.Add(cells);
        }

        // Build the Markdown table.
        var sb = new StringBuilder();

        // Header row.
        for (var i = 0; i < fieldCount; i++)
        {
            // Pad header to the maximum width of the column.
            sb.Append("| ").Append(headers[i].PadRight(maxWidths[i])).Append(" ");
        }
        sb.AppendLine("|");

        // Separator row.
        for (var i = 0; i < fieldCount; i++)
        {
            // Create a separator with dashes matching the column width.
            sb.Append("| ").Append(new string('-', maxWidths[i])).Append(" ");
        }
        sb.AppendLine("|");

        // Data rows.
        foreach (var row in rows)
        {
            for (var i = 0; i < fieldCount; i++)
            {
                sb.Append("| ").Append(row[i].PadRight(maxWidths[i])).Append(" ");
            }
            sb.AppendLine("|");
        }

        return sb.ToString();
    }
}

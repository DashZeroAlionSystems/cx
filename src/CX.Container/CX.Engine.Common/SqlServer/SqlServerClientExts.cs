using System.Data;
using System.Data.Common;

namespace CX.Engine.Common.SqlServer;

public static class SqlServerClientExts
{
    public static DbDataReader ToSqlDataReader<T>(this IEnumerable<T> rows)
    {
        var table = new DataTable();

        // 1. Gather all column names and types from each DapperRow.
        //    Each DapperRow implements IDictionary<string, object>.
        var allColumnNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // First pass: discover columns
        foreach (var row in rows)
        {
            if (row is IDictionary<string, object> dict)
            {
                foreach (var key in dict.Keys)
                {
                    // If the column is new, figure out its type from the first non-null value.
                    if (!allColumnNames.Contains(key))
                    {
                        allColumnNames.Add(key);
                    }
                }
            }
        }

        // Now add columns to the DataTable (we can’t know the perfect type if it’s null in the first row,
        // so we’ll default to object if needed).
        foreach (var colName in allColumnNames)
        {
            table.Columns.Add(colName, typeof(object));
        }

        // 2. Populate the DataTable rows
        foreach (var row in rows)
        {
            if (row is IDictionary<string, object> dict)
            {
                var dataRow = table.NewRow();
                foreach (var key in dict.Keys)
                {
                    dataRow[key] = dict[key] ?? DBNull.Value;
                }
                table.Rows.Add(dataRow);
            }
        }

        // 3. Return a DataTableReader, which implements IDataReader.
        //    (Note: this is not a SqlDataReader, but it behaves similarly for most use cases.)
        return table.CreateDataReader();
    }
}
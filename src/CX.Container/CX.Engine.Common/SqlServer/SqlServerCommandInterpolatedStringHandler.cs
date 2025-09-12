using System.Data.Common;
using CX.Engine.Common.PostgreSQL;
using Microsoft.Data.SqlClient;

namespace CX.Engine.Common.SqlServer;

using System.Runtime.CompilerServices;
using System.Text;
using Npgsql;

[InterpolatedStringHandler]
public struct SqlServerCommandInterpolatedStringHandler
{
    private readonly StringBuilder _builder;
    private readonly SqlCommand _command;
    private int _argNo;

    public SqlServerCommandInterpolatedStringHandler(int literalLength, int formattedCount)
    {
        _builder = new(literalLength + formattedCount * 20);
        _command = new();
    }

    public void AppendLiteral(string s)
    {
        _builder.Append(s);
    }

    public void AppendFormatted<T>(T t, string format) where T : IFormattable
    {
        _builder.Append(t?.ToString(format, null));
    }

    public void AppendFormatted<T>(T t)
    {
        if (t is InjectRaw raw)
        {
            _builder.Append(raw.Content);
            return;
        }

        //Npgsql boxes anyway, so we can do so without creating extra allocations here.
        //Npgsql does not handle null, instead it expects DBNull.Value.
        var value = t ?? (object)DBNull.Value;

        string argName = null;

        //Find an argument with the same value, if already present
        foreach (DbParameter param in _command.Parameters)
            if (MiscHelpers.AreEqual(value, param.Value!))
                argName = param.ParameterName;

        if (argName == null)
        {
            _argNo++;
            argName = $"@{_argNo}";
            
            _command.Parameters.AddWithValue(argName, value);
        }

        _builder.Append(argName);
    }

    public SqlCommand GetCommand()
    {
        _command.CommandText = _builder.ToString();
        return _command;
    }
}
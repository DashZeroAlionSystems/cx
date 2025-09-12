using System.Runtime.CompilerServices;
using System.Text;
using JetBrains.Annotations;
using Npgsql;

namespace CX.Engine.Common.PostgreSQL;

[InterpolatedStringHandler]
public struct NpgsqlCommandInterpolatedStringHandler
{
    private readonly StringBuilder _builder;
    private readonly NpgsqlCommand _command;
    private int _argNo;

    public NpgsqlCommandInterpolatedStringHandler(int literalLength, int formattedCount)
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
        foreach (NpgsqlParameter param in _command.Parameters)
            if (MiscHelpers.AreEqual(value, param.Value!))
                argName = param.ParameterName;

        if (argName == null)
        {
            _argNo++;
            argName = $"@{_argNo}";
            
            if (value is DateTime dt)
                _command.Parameters.AddWithValue(argName, NpgsqlTypes.NpgsqlDbType.TimestampTz, dt.ToUniversalTime());
            else if (value is Guid g)
                _command.Parameters.AddWithValue(argName, NpgsqlTypes.NpgsqlDbType.Uuid, g);
            else
                _command.Parameters.AddWithValue(argName, value);
        }

        _builder.Append(argName);
    }

    public NpgsqlCommand GetCommand()
    {
        _command.CommandText = _builder.ToString();
        return _command;
    }

    public static NpgsqlCommand GetCommand([LanguageInjection("SQL")] NpgsqlCommandInterpolatedStringHandler cmdString)
    {
        return cmdString.GetCommand();
    }
}
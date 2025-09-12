using System.Text.RegularExpressions;
using CX.Engine.Common.CodeProcessing;
using Npgsql;

namespace CX.Engine.Common.PostgreSQL;

public static class NpgsqlCommandStringFormatter
{
    public static async Task<NpgsqlCommand> FormatAsync(string s, object context)
    {
        var cmd = new NpgsqlCommand();
        var processedCommand = s;
        var idxOfset = 0;
        var argId = 1;
        //find all {expression} in the string
        var matches = Regex.Matches(s, @"\{([^}]+)\}");
        foreach (Match match in matches)
        {
            var matchString = match.ToString();
            //remove first and last character
            matchString = matchString.Substring(1, matchString.Length - 2);

            if (matchString == "'\\{'")
            {
                processedCommand = processedCommand.Remove(match.Index + idxOfset, match.Length);
                processedCommand = processedCommand.Insert(match.Index + idxOfset, "{");
                idxOfset += 1 - match.Length;
                continue;
            }

            if (matchString.StartsWith("$"))
            {
                var value = Accessor.EvaluateAsync(matchString.Substring(1), context)?.ToString() ?? "";
                processedCommand = processedCommand.Remove(match.Index + idxOfset, match.Length);
                processedCommand = processedCommand.Insert(match.Index + idxOfset, value);
                idxOfset += value.Length - match.Length;
                continue;
            }

            if (matchString.Contains(":"))
            {
                var nameAndValue = matchString.SplitAtFirst(":");
                
                if (!nameAndValue.part.StartsWith("@"))
                    continue;
                
                var parName = nameAndValue.part.Substring(1);
                if (cmd.Parameters.Contains(parName))
                    throw new InvalidOperationException($"Parameter {parName} already exists in the command");

                var value = Accessor.EvaluateAsync(nameAndValue.remainder, context);
                cmd.Parameters.AddWithValue(parName, value);

                processedCommand = processedCommand.Remove(match.Index + idxOfset, match.Length);
                processedCommand = processedCommand.Insert(match.Index + idxOfset, $"@{parName}");
                idxOfset += parName.Length + 1 - match.Length;
            }
            else
            {
                var parName = "arg" + argId++;
                if (cmd.Parameters.Contains(parName))
                    throw new InvalidOperationException($"Parameter {parName} already exists in the command");

                var value = await Accessor.EvaluateAsync(matchString, context) ?? DBNull.Value;
                cmd.Parameters.AddWithValue(parName, value);

                processedCommand = processedCommand.Remove(match.Index + idxOfset, match.Length);
                processedCommand = processedCommand.Insert(match.Index + idxOfset, $"@{parName}");
                idxOfset += parName.Length + 1 - match.Length;
            }
        }

        cmd.CommandText = processedCommand;
        return cmd;
    }
}
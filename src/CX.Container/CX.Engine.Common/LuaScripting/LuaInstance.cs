using System.Text;
using Microsoft.Extensions.Logging;
using MoonSharp.Interpreter;

namespace CX.Engine.Common;

public class LuaInstance
{
    public readonly object Lock = new();
    public readonly Script Script = new();
    public readonly Dictionary<string, (string resolvesTo, string description)> Shortcuts = new(StringComparer.InvariantCultureIgnoreCase);
    public ILogger Logger;
    public StringBuilder Output = new();

    public LuaInstance(ILogger logger)
    {
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void PrintLine(Exception ex, string where)
    {
        PrintLine("Exception at " + where + "\r\n" + ex.GetExceptionDetails());
    }

    public void PrintLine(string s = "")
    {
        lock (Lock)
        {
            if (Logger != null)
                Logger.LogInformation(s);
            Output.AppendLine(s);
        }
    }

    public async Task<string> RunAsync(string cmd)
    {
        Output = new();
        Script.Globals["lua"] = this;

        if (string.IsNullOrWhiteSpace(cmd))
            return "";

        if (Shortcuts.TryGetValue(cmd, out var shortcut))
            cmd = shortcut.resolvesTo;

        try
        {
            var result = Script.DoString(cmd);

            if (result.Type == DataType.UserData)
            {
                var obj = result.UserData.Object;

                obj = await MiscHelpers.AwaitAnyAsync(obj);

                if (obj.IsVoid())
                    result = DynValue.Void;
                else
                    result = DynValue.FromObject(Script, obj);
            }

            if (result.IsNotVoid())
                PrintLine(result.ToPrintString());
            else
                PrintLine("<void>");

            return Output.ToString();
        }
        catch (Exception ex)
        {
            PrintLine("EXCEPTION:\r\n" + ex.GetExceptionDetails());
            return Output.ToString();
        }
    }
}
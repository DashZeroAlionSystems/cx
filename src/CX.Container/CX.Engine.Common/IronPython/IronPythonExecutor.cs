using System.Text;
using CX.Engine.Common.IronPython;
using IronPython.Hosting;
using Microsoft.Scripting.Hosting;

public static class IronPythonExecutor
{
    public static Task<dynamic> ExecuteScriptAsync(string cmd, object args) => ExecuteScriptAsync(new(cmd, args));
    public static Task<dynamic> ExecuteScriptAsync(string cmd, IronPythonContext ctx)
    {
        if (ctx == null)
            throw new ArgumentNullException(nameof(ctx));
        
        return ExecuteScriptAsync(ctx.GetRequest(cmd));
    }
    public static async Task ExecuteScriptsAsync(List<IronPythonScript> scripts)
    {
        if (scripts.Any(s => string.IsNullOrWhiteSpace(s.Script))) 
            throw new ArgumentException("A script in the list provided cannot be null or empty.", nameof(scripts));

        var engine = Python.CreateEngine();
        await Task.WhenAll(scripts.Select(s => CompileExecuteScriptAsync(engine, s)));
    }
    private static async Task CompileExecuteScriptAsync(ScriptEngine scriptEngine, IronPythonScript pyScript)
    {
        await Task.Run(() =>
        {
            pyScript.AddScope?.Invoke(scriptEngine);
            pyScript.PrepareScope?.Invoke();
            pyScript.CompileScript?.Invoke(scriptEngine);
            pyScript.RunScript();
        });
    }

    /// <summary>
    /// Executes an IronPython script and passes a C# object to it.
    /// </summary>
    /// <param name="req">The request to send to IronPython.</param>
    public static async Task<dynamic> ExecuteScriptAsync(IronPythonRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Script))
            throw new ArgumentException("The script cannot be null or empty.", nameof(req));
        
        // Create a new Python runtime environment
        var engine = Python.CreateEngine();
        req.SetupEngine?.Invoke(engine); 
        
        var scope = engine.CreateScope();

        req.SetupScope?.Invoke(scope);

        var sb = new StringBuilder();

        foreach (var import in req.WildcardImports)
            sb.AppendLine($"from {import} import *");

        foreach (var import in req.Imports)
            if (import.StartsWith("from "))
                sb.AppendLine(import);
            else
                sb.AppendLine($"import {import}");

        foreach (var import in req.ImportExtensions)
            sb.AppendLine($"clr.ImportExtensions({import})");

        sb.AppendLine(req.Script);

        var res = engine.Execute(sb.ToString(), scope);

        if (res is Task<object> t)
            res = await t;

        if (scope.ContainsVariable("result"))
            res = scope.GetVariable("result");
        
        return res;
    }
}
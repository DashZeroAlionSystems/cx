using System.Text;
using Microsoft.Scripting.Hosting;

namespace CX.Engine.Common.IronPython;

public class IronPythonScript
{
    public string Core { get; set; } = @"import clr 
clr.AddReference('System.Text.Json') 
clr.AddReference('CX.Engine.Common') ";
    public string Modules { get; set; } = @"from System.Text.Json import * 
from System.Text.Json.Nodes import * 
from System.Threading.Tasks import * 
from System import * 
from System.Collections.Generic import * 
from CX.Engine.Common.IronPython import IronPythonAssist as cs ";
    public string Extensions { get; set; } = string.Empty;

    public string PreparedScript => GetPreparedScript();
    public Action<ScriptEngine> AddScope => OnAddScope;
    public Action<ScriptScope> SetScope => OnSetScope;
    public Action<ScriptEngine> CompileScript => OnCompileScript;
    public Action PrepareScope => OnPrepareScope;
    public Action<string[]> ImportExtensions => OnImportExtensions;
    private ScriptScope _scope {  get; set; }
    public ScriptScope Scope { get =>  _scope; }
    private CompiledCode _compiledScript { get; set; }
    public CompiledCode CompiledScript { get => _compiledScript; }
    private Dictionary<string, object> _scopeVariables { get; set; } = [];
    public Dictionary<string, object> ScopeVariables
    {
        get => _scopeVariables;
        set => _scopeVariables = value;
    }
    private string _script { get; set; }
    public string Script
    {
        get => _script;
        set => _script = value;
    }

    private string GetPreparedScript()
    {
        StringBuilder sb = new();
        sb.AppendLine(Core);
        sb.AppendLine(Modules);
        sb.AppendLine(Extensions);
        return sb.ToString();
    }
    private void OnAddScope(ScriptEngine engine)
    {
        _scope = engine.CreateScope();
    }
    private void OnSetScope(ScriptScope scope)
    {
        _scope = scope;
    }
    private void OnCompileScript(ScriptEngine engine)
    {
        StringBuilder sb = new();
        sb.AppendLine(PreparedScript);
        sb.AppendLine(Script);
        var source = engine.CreateScriptSourceFromString(sb.ToString());
        _compiledScript = source.Compile();
    }
    private void OnImportExtensions(string[] extensions)
    {
        StringBuilder sb = new();
        sb.AppendLine(Extensions);
        foreach (var ext in extensions)
        {
            sb.AppendLine($"clr.ImportExtensions({ext})");
        }
        Extensions = sb.ToString();
    }
    private void OnPrepareScope()
    {
        foreach (var (key, value) in ScopeVariables) 
            Scope.SetVariable(key, value);
    }
    public dynamic RunScript()
    {
        return CompiledScript.Execute(Scope);
    }
}
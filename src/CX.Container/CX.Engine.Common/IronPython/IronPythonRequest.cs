using JetBrains.Annotations;
using Microsoft.Scripting.Hosting;

namespace CX.Engine.Common.IronPython;

public class IronPythonRequest
{
    public static List<string> GetDefaultWildcardImports() => ["System.Text.Json", "System.Text.Json.Nodes", "System.Threading.Tasks", "System", "System.Collections.Generic" ];

    public static List<string> GetDefaultImports() => ["from CX.Engine.Common.IronPython import IronPythonAssist as cs", "clr"];
    
    public List<string> WildcardImports = GetDefaultWildcardImports();
    public List<string> Imports = GetDefaultImports();
    public List<string> ImportExtensions = [];
    public string Script;
    public Action<ScriptScope> SetupScope;
    public Action<ScriptEngine> SetupEngine;

    public IronPythonRequest()
    {
        SetupStandardEngine();
    }
    
    public static void SetupStandardEngineHandler(ScriptEngine engine) => engine.Execute("""
                                                                                         import clr
                                                                                         clr.AddReference('System.Text.Json')
                                                                                         clr.AddReference('CX.Engine.Common')
                                                                                         """);

    public IronPythonRequest SetupStandardEngine()
    {
        SetupEngine = SetupStandardEngineHandler;
        return this;
    }

    public IronPythonRequest([NotNull] string script)
    {
        SetupStandardEngine();
        Script = script ?? throw new ArgumentNullException(nameof(script));
    }

    public IronPythonRequest([NotNull] string script, object args)
    {
        SetupStandardEngine();
        Script = script ?? throw new ArgumentNullException(nameof(script));
        WithArguments(args);
    }

    public IronPythonRequest WithArgument(string name, object value)
    {
        void Local_AddVariable(ScriptScope scope) => scope.SetVariable(name, value);
        SetupScope += Local_AddVariable;
        return this;
    }
    
    public IronPythonRequest AddVariable(string name, object value) => WithArgument(name, value);
    public IronPythonRequest AddVariables(params (string name, object value)[] vars) => WithArguments(vars);
    public IronPythonRequest AddVariables(IDictionary<string, object> vars) => WithArguments(vars);

    public IronPythonRequest WithArguments(params (string name, object value)[] args)
    {
        void Local_AddVariables(ScriptScope scope)
        {
            foreach (var (name, value) in args)
                scope.SetVariable(name, value);
        }

        SetupScope += Local_AddVariables;
        return this;
    }

    public IronPythonRequest WithArguments(object args)
    {
        void Local_AddVariables(ScriptScope scope)
        {
            if (args is IDictionary<string, object> dict)
            {
                foreach (var (key, value) in dict)
                    scope.SetVariable(key, value);
                return;
            }

            foreach (var prop in args.GetType().GetProperties())
                scope.SetVariable(prop.Name, prop.GetValue(args));

            foreach (var field in args.GetType().GetFields())
                scope.SetVariable(field.Name, field.GetValue(args));
        }

        SetupScope += Local_AddVariables;
        return this;
    }

    public static implicit operator IronPythonRequest(string script) => new(script);
}
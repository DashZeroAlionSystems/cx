using Microsoft.Scripting.Hosting;

namespace CX.Engine.Common.IronPython;

public class IronPythonContext
{
    public List<string> WildcardImports = IronPythonRequest.GetDefaultWildcardImports();
    public List<string> Imports = IronPythonRequest.GetDefaultImports();
    public Action<ScriptScope> SetupScope;
    public Action<ScriptEngine> SetupEngine = IronPythonRequest.SetupStandardEngineHandler;
    public Dictionary<string, object> Variables = new();

    public IronPythonRequest GetRequest(string script = null)
    {
        var req =  new IronPythonRequest
        {
            WildcardImports = WildcardImports,
            SetupEngine = SetupEngine,
            SetupScope = SetupScope,
            Script = script
        };
        req.AddVariables(Variables);
        return req;
    }
    
    public static IronPythonContext GetDefaultContext() => new();

    public IronPythonContext AddVariable<T>(string name, T value)
    {
        Variables.Add(name, (object)value);
        return this;
    }

    public IronPythonContext AddMethod(Delegate method)
    {
        AddVariable(MiscHelpers.CleanMethodName(method.Method.Name), method);
        return this;
    }

    public IronPythonContext AddMethods(params Delegate[] methods)
    {
        foreach (var method in methods)
            AddMethod(method);
        return this;
    }
}
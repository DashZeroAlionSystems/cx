using CX.Engine.Common.CodeProcessing;
using CX.Engine.Common.Conversion;
using CX.Engine.Common.Formatting;
using HtmlAgilityPack;
using Microsoft.Scripting.Utils;

namespace CX.Engine.Common.Xml;

public class CxmlScope : CxSmartScope
{
    public bool SmartFormatEscape;
    public Dictionary<ICxmlComputeNode, Task> ComputeTasks = new();

    public CxmlScope()
    {
    }

    public CxmlScope(params Delegate[] actions)
    {
        SetActions(actions);
    }


    public CxmlScope(StubbedLazyDictionary context, params Delegate[] actions) : base(context)
    {
        SetActions(actions);
    }

    public CxmlScope(object context, params Delegate[] actions) : base(context)
    {
        SetActions(actions);
    }

    public CxmlScope Inherit()
    {
        var res = new CxmlScope();
        res.Root = Root;
        res.Parent = this;
        res.RenderErrors = RenderErrors;
        res.Preparation = Preparation;
        res.ComputeTasks = ComputeTasks;
        res.SmartFormatEscape = SmartFormatEscape;
        return res;
    }

    public CxmlPreparation Preparation = new();
    
    /// <summary>
    /// NB: Not inherited by child scopes.
    /// </summary>
    public Delegate TopLevelNodeHandler;

    public void SetActions(params Delegate[] methods) => SetActions(methods.AsEnumerable());

    public void SetActions(IEnumerable<Delegate> methods)
    {
        Preparation = new();
        TopLevelNodeHandler = CxmlCommon.ContainerNode;
        
        //get all method names from methods into a List<string>
        foreach (var method in methods)
        {
            string methodName = null;

            var attr = method.Method.GetCustomAttribute<CxmlActionAttribute>();
            methodName = attr?.Name;

            methodName ??= MiscHelpers.CleanMethodName(method.Method.Name).ToKebabCase();

            methodName = methodName.ToLowerInvariant();

            if (!Preparation.Delegates.TryAdd(methodName, method))
                throw new ArgumentException($"Method {methodName} is already defined.");

            Preparation.Actions.Add(method);
            Preparation.MethodNames.Add(methodName);
        }
    }

    public async Task ComputeAsync(ICxmlComputeNode node)
    {
        await ComputeOperation.Start("Cannot compute a node while already in a compute operation.  Use dependencies to handle these scenarios.", ComputeTasks)
            .WalkInStartedAsync(node,
                n => n.ResolveDependencies<ICxmlComputeNode>(),
                n => n.InternalComputeAsync(this));
    }

    public async Task<object> ResolveReferenceFieldValueAsync(object o) => await ResolveReferenceFieldValueAsync(typeof(object), o);
    public async Task<T> ResolveReferenceFieldValueAsync<T>(object o) => (T)await ResolveReferenceFieldValueAsync(typeof(T), o);

    /// <summary>
    /// Resolves a reference field's value (one that may contain a reference or an object).
    /// If the object is already assignable to the type it is returned unmodified.  Otherwise, it is cast to a string and treated as an accessor (reference).
    /// If the target type is object (open-ended) strings and HtmlNodes are still treated as references.
    /// </summary>
    public async Task<object> ResolveReferenceFieldValueAsync(Type t, object o)
    {
        if (t == typeof(object) && o is not string && o is not HtmlNode)
            return o;
        else
        if (t != typeof(object) && o.GetType().IsAssignableTo(t))
            return o;
        
        var accessor = await CxConvert.ToAsync<string>(o);
                        
        if (accessor == null)
            return default;

        var val = await Accessor.EvaluateAsync(accessor, this);

        if (val == null)
            return default;

        if (val.GetType().IsAssignableTo(t))
            return val;

        return default;
    }

    public class ComputeOperation : CxmlDependencyTracker<ComputeOperation, ICxmlComputeNode>;
}
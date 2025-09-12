using CX.Engine.Common.Xml;

namespace CX.Engine.Common.Formatting;

public class CxSmartScope : IResolveValueAsync
{
    public readonly Guid Id = Guid.NewGuid();
    public readonly StubbedLazyDictionary Context;
    
    public CxSmartScope Root;
    public CxSmartScope Parent;
    public bool RenderErrors = true;
    
    public object this[string key]
    {
        set => Context[key] = value;
    }
    
    public override string ToString()
    {
        return Id.ToString();
    }

    public Task<object> ResolveValueAsync(string key, bool optional = false) => ResolveValueAsync<object>(key, optional);
    
    public async Task<T> ResolveValueAsync<T>(string key, bool optional = false)
    {
        if (Context.ContainsKey(key))
            return (T)await Context.ResolveValueAsync(key);
        else if (Parent != null)
            return (T)await Parent.ResolveValueAsync(key, optional);
        
        return default;
    }

    public void SetToStubMode()
    {
        Context.SetToStubMode();
        Parent?.SetToStubMode();
    }

    public async Task ResolveAsync()
    {
        await Context.ResolveAsync();
        if (Parent != null)
            await Parent.ResolveAsync();
    }

    public async Task<T> ResolveValueOrDefaultAsync<T>(string key)
    {
        if (Context.ContainsKey(key))
            return await Context.ResolveValueOrDefaultAsync<T>(key);
        else if (Parent != null)
            return await Parent.ResolveValueOrDefaultAsync<T>(key);
        
        return default;
    }

    public CxSmartScope(StubbedLazyDictionary context)
    {
        Context = context ?? new();
        Root = this;
    }
    
    public CxSmartScope(object context)
    {
        Context = context as StubbedLazyDictionary ?? new(context);
        Root = this;
    }

    public CxSmartScope()
    {
        Context = new();
        Root = this;
    }

    /// <summary>
    /// Returns a string if errors should be rendered.
    /// Throws a <see cref="CxmlException"/> if errors should be thrown.
    /// </summary>
    /// <returns></returns>
    public string ErrorString(string s)
    {
        if (RenderErrors)
            return $"<exception><message>{s}</message></exception>";
        else
            throw new CxmlException(s);
    }

    public StubbedLazyDictionary GetFullContext()
    {
        var res = new StubbedLazyDictionary();
        var ancestors = new List<CxSmartScope>();
        var current = this;
        while (current != null)
        {
            ancestors.Add(current);
            current = current.Parent;
        }

        for (var i = ancestors.Count - 1; i >= 0; i--)
        {
            var ancestor = ancestors[i];
            foreach (var prop in ancestor.Context.Props)
                res.Props[prop.Key] = prop.Value;
        }

        return res;
    }
}
using System.Collections;

namespace CX.Engine.Common.Xml;

public class NodeSelector : IResolveValueAsync, IDictionary<string, object>
{
    public readonly ICxmlChildren Root;

    public NodeSelector(ICxmlChildren root)
    {
        Root = root ?? throw new ArgumentNullException(nameof(root));
    }

    public object ResolveValue(string key, bool optional = false)
    {
        var matches = Root.DescendantsWithId(key).ToFirstElementOrList();
        
        if (!optional && matches == null)
            throw new InvalidOperationException($"No node with id '{key}' found.");
        
        return matches;
    }

    public Task<object> ResolveValueAsync(string key, bool optional = false) => Task.FromResult(ResolveValue(key, optional));

    public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
    {
        foreach (var kvp in Root.DescendantsWithIds())
            yield return kvp;
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public void Add(KeyValuePair<string, object> item)
    {
        throw new NotSupportedException();
    }

    public void Clear()
    {
        throw new NotSupportedException();
    }

    public bool Contains(KeyValuePair<string, object> item)
    {
        foreach (var kvp in this)
            if (kvp.Key == item.Key && kvp.Value == item.Value)
                return true;

        return false;
    }

    public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex)
    {
        throw new NotSupportedException();
    }

    public bool Remove(KeyValuePair<string, object> item)
    {
        throw new NotSupportedException();
    }

    public int Count => Root.DescendantsWithIds().Count();
    public bool IsReadOnly => true;

    public void Add(string key, object value)
    {
        throw new NotSupportedException();
    }

    public bool ContainsKey(string key)
    {
        return ResolveValue(key) != null;
    }

    public bool Remove(string key)
    {
        throw new NotSupportedException();
    }

    public bool TryGetValue(string key, out object value)
    {
        var val = ResolveValue(key);
        return (value = val) != null;
    }

    public object this[string key]
    {
        get => ResolveValue(key);
        set => throw new NotSupportedException();
    }

    public ICollection<string> Keys => Root.DescendantsWithIds().Select(kvp => kvp.Key).ToList();

    public ICollection<object> Values => Root.DescendantsWithIds().Select(kvp => kvp.Value).ToList();
}
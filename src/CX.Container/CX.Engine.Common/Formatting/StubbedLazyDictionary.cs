using System.Collections;
using System.Reflection;

namespace CX.Engine.Common.Formatting;

public static class StubbedLazyDictionaryExtensions
{
    public static StubbedLazyDictionary ToStubbedLazyDictionary(this IDictionary<string, object> dict)
    {
        var res = new StubbedLazyDictionary();
        foreach (var key in dict.Keys)
            res.Add(key, dict[key]);
        return res;
    }
}

public class StubbedLazyDictionary : IDictionary<string, object>, IResolveValueAsync
{
    public enum ValueMode
    {
        Init = 0,
        Stub = 1,
        Resolved = 2
    };

    private ValueMode _mode = ValueMode.Init;
    public readonly Dictionary<string, StubbedLazyValue> Props = new();

    public ValueMode Mode => _mode;

    public Func<string, StubbedLazyValue> PropFactory;

    /// <summary>
    /// Recursively sets all properties to stub mode.
    /// </summary>
    public void SetToStubMode()
    {
        if (_mode == ValueMode.Stub)
            return;

        var prevMode = _mode;

        //NB: has to happen before setting Props to prevent infinite recursion
        _mode = ValueMode.Stub;
        try
        {
            foreach (var kvp in Props)
            {
                kvp.Value.UsedValue = false;
                kvp.Value.Resolved = false;
                kvp.Value.LastResolvedValue = null;
                if (kvp.Value.DoGetStub() is StubbedLazyDictionary sld)
                    sld.SetToStubMode();
            }
        }
        catch
        {
            _mode = prevMode;
            throw;
        }
    }

    public void SetToInitMode(bool onlyIfNotAlreadyInInitMode = true)
    {
        if (_mode == ValueMode.Init && onlyIfNotAlreadyInInitMode)
            return;

        var prevMode = _mode;
        //NB: has to happen before setting Props to prevent infinite recursion
        _mode = ValueMode.Init;

        try
        {
            foreach (var kvp in Props)
            {
                kvp.Value.UsedValue = false;
                kvp.Value.Resolved = false;
                kvp.Value.LastResolvedValue = null;
                if (kvp.Value.DoGetStub() is StubbedLazyDictionary sld)
                    sld.SetToInitMode(onlyIfNotAlreadyInInitMode);
            }
        }
        catch
        {
            _mode = prevMode;
            throw;
        }
    }

    /// <summary>
    /// Recursively sets all properties to resolved mode.
    /// </summary>
    public async Task ResolveAsync()
    {
        if (_mode == ValueMode.Resolved)
            return;

        var prevMode = _mode;

        //NB: has to happen before setting Props to prevent infinite recursion
        _mode = ValueMode.Resolved;

        try
        {
            await (from kvp in Props where kvp.Value.UsedValue select kvp.Value.ResolveValueAsync());

            var tasks = new List<Task>();

            foreach (var kvp in Props)
                if (kvp.Value.UsedValue && kvp.Value.LastResolvedValue is StubbedLazyDictionary sld)
                    tasks.Add(sld.ResolveAsync());

            await tasks;
        }
        catch
        {
            _mode = prevMode;
            throw;
        }
    }

    public async Task<object> ResolveValueAsync(string key, bool optional = false)
    {
        if (!Props.TryGetValue(key, out var slv) && !TryFactory(key, out slv))
        {
            if (!optional)
                throw new KeyNotFoundException($"Key {key} not found in dictionary.");
            else
                return null;
        }

        if (slv.Resolved && slv.Cache)
            return slv.LastResolvedValue;

        return await slv.ResolveValueAsync();
    }

    public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
    {
        if (_mode == ValueMode.Init)
            foreach (var kvp in Props)
               yield return new(kvp.Key, kvp.Value?.Value ?? kvp.Value);
        
        if (Mode == ValueMode.Stub)
            foreach (var kvp in Props)
            {
                kvp.Value.UsedValue = true;
                yield return new(kvp.Key, kvp.Value.DoGetStub());
            }
        else
            foreach (var kvp in Props)
            {
                kvp.Value.UsedValue = true;
                yield return new(kvp.Key, kvp.Value.LastResolvedValue);
            }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }


    public StubbedLazyDictionary()
    {
    }

    public StubbedLazyDictionary(IDictionary<string, object> dict)
    {
        ArgumentNullException.ThrowIfNull(dict);

        foreach (var kvp in dict)
            Add(kvp);
    }


    public StubbedLazyDictionary(StubbedLazyDictionary dict)
    {
        ArgumentNullException.ThrowIfNull(dict);

        foreach (var kvp in dict)
            Add(kvp);
    }

    public StubbedLazyDictionary(object o)
    {
        if (o == null)
            return;

        if (o is IDictionary<string, object> dict)
        {
            foreach (var kvp in dict)
                Add(kvp);
            return;
        }

        var type = o.GetType();

        if (type.IsArray)
            throw new ArgumentException("Cannot create a dictionary from an array.");

        foreach (var fld in type.GetFields(BindingFlags.Instance | BindingFlags.Public))
            Add(fld.Name, fld.GetValue(o));

        foreach (var fld in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            Add(fld.Name, fld.GetValue(o));
    }


    public void Add(KeyValuePair<string, object> item)
    {
        if (_mode != ValueMode.Init)
            throw new InvalidOperationException("Cannot add a value to a dictionary in stub or resolved mode.");

        Props.Add(item.Key, new(item.Value));
    }

    public void Clear()
    {
        if (_mode != ValueMode.Init)
            throw new InvalidOperationException("Cannot clear a dictionary in stub or resolved mode.");

        Props.Clear();
    }

    private bool TryFactory(string key, out StubbedLazyValue val)
    {
        if (Mode != ValueMode.Init)
        {
            val = default;
            return false;
        }

        if (PropFactory == null)
        {
            val = default;
            return false;
        }

        val = PropFactory(key);
        Props[key] = val;
        return true;
    }

    public bool Contains(KeyValuePair<string, object> item)
    {
        if (!Props.TryGetValue(item.Key, out var prop))
        {
            if (!TryFactory(item.Key, out prop))
                return false;
        }

        if (Mode == ValueMode.Init)
            return prop.Value == item.Value || (prop.Value is StubbedLazyValue slv && slv.Value == item.Value && slv.GetValue == null && slv.GetStub == null);
        else if (Mode == ValueMode.Stub)
            return prop.DoGetStub() == item.Value;
        else
            return prop.LastResolvedValue == item.Value;
    }

    public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex)
    {
        if (_mode == ValueMode.Init)
            throw new InvalidOperationException("Cannot get value from a dictionary in init mode.");

        foreach (var kvp in this)
            array[arrayIndex++] = kvp;
    }

    public bool Remove(KeyValuePair<string, object> item)
    {
        if (Contains(item))
        {
            Remove(item.Key);
            return true;
        }

        return false;
    }

    public int Count => Props.Count;
    public bool IsReadOnly => false;

    public void Add(string key, object value)
    {
        if (_mode != ValueMode.Init)
            throw new InvalidOperationException("Cannot add a value to a dictionary in stub or resolved mode.");

        Props.Add(key, BoxValue(value));
    }

    public bool ContainsKey(string key)
    {
        var contains = Props.ContainsKey(key);

        if (!contains && TryFactory(key, out _))
            return true;

        return contains;
    }

    public bool Remove(string key)
    {
        if (Props.Remove(key))
            return true;

        return false;
    }

    public T GetValueOrDefault<T>(string key, T defaultValue = default) =>
        TryGetValue(key, out var value) ? (T)value : defaultValue;

    public Task<T> ResolveValueOrDefaultAsync<T>(string key, T defaultValue = default)
    {
        if (Props.ContainsKey(key))
            return ResolveValueAsync(key).ContinueWith(t => (T)t.Result);
        else if (TryFactory(key, out var slv))
            return slv.ResolveValueAsync().ContinueWith(t => (T)t.Result);
        else
            return Task.FromResult(defaultValue);
    }

    public object GetValueOrDefault(string key, object defaultValue = null) =>
        TryGetValue(key, out var value) ? value : defaultValue;

    public bool TryGetValue<T>(string key, out T value)
    {
        if (!TryGetValue(key, out var obj))
        {
            value = default;
            return false;
        }

        value = (T)obj;
        return true;
    }

    public bool TryGetValue(string key, out object value)
    {
        if (!Props.TryGetValue(key, out var prop))
        {
            if (!TryFactory(key, out prop))
            {
                value = default;
                return false;
            }
        }
        
        if (Mode == ValueMode.Init)
        {
            if (prop.Resolved && prop.Cache)
            {
                value = prop.Value;
                return true;
            }

            if (prop.GetValue != null)
                value = prop.GetValue();
            else if (prop.GetValueAsync != null)
                value = prop.GetValueAsync();
            
            value = prop.Value;
            return true;
        }

        prop.UsedValue = true;
        value = Mode == ValueMode.Stub ? prop.DoGetStub() : prop.LastResolvedValue;
        return true;
    }

    public static StubbedLazyValue BoxValue(object value)
    {
        if (value is StubbedLazyValue slv)
            return slv;
        else
            return new(value);
    }

    private void SetValue(string key, object value)
    {
        Props[key] = BoxValue(value);
    }

    public object this[string key]
    {
        get
        {
            if (!TryGetValue(key, out var value))
                throw new KeyNotFoundException($"Key {key} not found in dictionary.");

            return value;
        }
        set => SetValue(key, value);
    }

    public ICollection<string> Keys => Props.Keys;

    public ICollection<object> Values =>
        Props.Values.Select(p =>
        {
            p.UsedValue = true;
            return p.LastResolvedValue;
        }).ToList();
}
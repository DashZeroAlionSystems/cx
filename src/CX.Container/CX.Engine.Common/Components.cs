using System.Collections;

namespace CX.Engine.Common;

public sealed class Components<TComponent>: IEnumerable<TComponent> 
{
    private readonly List<TComponent> _items = [];
    
    public int Count => _items.Count;
    
    public TComponent this[int index] => _items[index];

    /// <summary>
    /// Returns the first component of type T.  If none are found, return default(T).
    /// </summary>
    public T GetValueOrDefault<T>()
    {
        foreach (var component in _items)
        {
            if (component is T t)
                return t;
        }

        return default;
    }
    
    /// <summary>
    /// Returns the first component of type T.  If none are found, return default(T).
    /// </summary>
    public TComponent GetComponentOrDefault(Type t)
    {
        foreach (var component in _items)
        {
            if (t.IsInstanceOfType(component))
                return component;
        }

        return default;
    }

    public bool TryGet<T>(out T component) 
    {
        foreach (var c in _items)
        {
            if (c is T t)
            {
                component = t;
                return true;
            }
        }

        component = default;
        return false;
    }

    public bool TryGet(Type t, out TComponent component)
    {
        if (_items == null)
        {
            component = default;
            return false;
        }

        foreach (var c in _items)
        {
            if (t.IsInstanceOfType(c))
            {
                component = c;
                return true;
            }
        }

        component = default!;
        return false;
    }

    public List<T> OfType<T>() 
    {
        var res = new List<T>();
        foreach (var component in _items)
        {
            if (component is T t)
                res.Add(t);
        }

        return res;
    }
    
    public List<TComponent> GetComponents(Type t)
    {
        var res = new List<TComponent>();
        foreach (var component in _items)
        {
            if (t.IsInstanceOfType(component))
                res.Add(component);
        }

        return res;
    }

    public void Add(TComponent component)
    {
        if (component == null)
            throw new ArgumentNullException(nameof(component));

        var t = component.GetType();
        if (Attribute.IsDefined(t, typeof(UniqueComponentAttribute)))
        {
            if (GetComponentOrDefault(t) != null)
                throw new InvalidOperationException($"Only one {t.FullName} component is allowed.");
        }

        _items.Add(component);
    }
    
    public void RemoveComponent<T>() where T: TComponent
    {
        for (var i = 0; i < _items.Count; i++)
        {
            if (_items[i] is T)
            {
                _items.RemoveAt(i);
                return;
            }
        }
    }
    
    /// <summary>
    /// Use after deserialization to validate for current rules.
    /// </summary>
    public void ValidateComponents()
    {
        foreach (var component in _items)
        {
            if (Attribute.IsDefined(component.GetType(), typeof(UniqueComponentAttribute)))
            {
                if (GetComponents(component.GetType()).Count > 1)
                    throw new InvalidOperationException($"Multiple instances of unique component {component.GetType().Name} found in configuration.");
            }
        }
    }

    public IEnumerator<TComponent> GetEnumerator()
    {
        return _items.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return _items.GetEnumerator();
    }

    public void Clear()
    {
        _items.Clear();
    }

    public void AddRange(IEnumerable<TComponent> range)
    {
        foreach (var component in range)
            Add(component);
    }
}
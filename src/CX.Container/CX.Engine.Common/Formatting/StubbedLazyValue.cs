using JetBrains.Annotations;

namespace CX.Engine.Common.Formatting;

public class StubbedLazyValue
{
    public readonly Func<ValueTask<object>> GetValueAsync;
    public readonly Func<object> GetValue;
    public readonly Func<object> GetStub;
    public object Value;
    public object LastResolvedValue;
    public bool UsedValue;
    public bool Resolved;
    public bool Cache = true;
    
    /// <summary>
    /// When no value is provided, GetValue and GetStub have to be provided.
    /// </summary>
    public StubbedLazyValue([NotNull] Func<Task<object>> getValueAsync, [NotNull] Func<object> getStub)
    {
        if (getValueAsync == null)
            throw new ArgumentNullException(nameof(getValueAsync));
        
        GetValueAsync = async () => await getValueAsync();
        GetStub = getStub ?? throw new ArgumentNullException(nameof(getStub));
    }

    /// <summary>
    /// When no value is provided, GetValue and GetStub have to be provided.
    /// </summary>
    public StubbedLazyValue([NotNull] Func<ValueTask<object>> getValueAsync, [NotNull] Func<object> getStub)
    {
        GetValueAsync = getValueAsync ?? throw new ArgumentNullException(nameof(getValueAsync));
        GetStub = getStub ?? throw new ArgumentNullException(nameof(getStub));
    }
       
    /// <summary>
    /// When no value is provided, GetValue and GetStub have to be provided.
    /// </summary>
    public StubbedLazyValue(Func<object> getValue, Func<object> getStub)
    {
        GetValue = getValue ?? throw new ArgumentNullException(nameof(getValue));
        GetStub = getStub ?? throw new ArgumentNullException(nameof(getStub));
    }

    /// <summary>
    /// When no value is provided, GetValue and GetStub have to be provided.
    /// </summary>
    public StubbedLazyValue(Func<object> getValue, object value)
    {
        GetValue = getValue;
        Value = value;
    }

    /// <summary>
    /// When a value is provided (including null, so not optional), GetValue and GetStub are optional and will take presedence over the value.
    /// </summary>
    public StubbedLazyValue(Func<ValueTask<object>> getValueAsync, Func<object> getStub, object value)
    {
        GetValueAsync = getValueAsync;
        GetStub = getStub;
        Value = value;
    }

    /// <summary>
    /// When a value is provided (including null, so not optional), GetValue and GetStub are optional and will take presedence over the value.
    /// </summary>
    public StubbedLazyValue(Func<ValueTask<object>> getValueAsync, object value)
    {
        GetValueAsync = getValueAsync;
        Value = value;
    }

    /// <summary>
    /// When a value is provided (including null, so not optional), GetValue and GetStub are optional and will take presedence over the value.
    /// </summary>
    public StubbedLazyValue(Func<Task<object>> getValueAsync, Func<object> getStub, object value)
    {
        if (getValueAsync != null)
            GetValueAsync = async () => await getValueAsync();
        
        GetStub = getStub;
        Value = value;
    }
    
    /// <summary>
    /// When a value is provided (including null, so not optional), GetValue and GetStub are optional and will take presedence over the value.
    /// </summary>
    public StubbedLazyValue(Func<Task<object>> getValueAsync, object value)
    {
        if (getValueAsync != null)
            GetValueAsync = async () => await getValueAsync();
        
        Value = value;
    }

    /// <summary>
    /// When a value is provided (including null, so not optional), GetValue and GetStub are optional and will take presedence over the value.
    /// </summary>
    public StubbedLazyValue(Func<object> getValue, Func<object> getStub, object value)
    {
        GetValue = getValue;
        GetStub = getStub;
        Value = value;
    }

    /// <summary>
    /// With only a value provided, GetValue and GetStub will never be called. 
    /// </summary>
    public StubbedLazyValue(object value)
    {
        Value = value;
    }

    public object DoGetStub()
    {
        if (GetStub != null)
        {
            LastResolvedValue = GetStub();
            return LastResolvedValue;
        }

        LastResolvedValue = Value;
        return Value;
    }
    
    public async Task<object> ResolveValueAsync()
    {
        Resolved = true;
        
        if (GetValue != null)
        {
            LastResolvedValue = GetValue();
            return LastResolvedValue;
        }

        if (GetValueAsync != null)
        {
            LastResolvedValue = await GetValueAsync();
            return LastResolvedValue;
        }

        LastResolvedValue = Value;
        return LastResolvedValue;
    }
    
    public static StubbedLazyValue FromValue(object value)
    {
        return new(value);
    }

    public static StubbedLazyValue FromFunc<T>(Func<T> func, T stubValue)
    {
        if (func is Func<object> fo)
            return new(fo, stubValue);
        else 
            return new(() => func(), stubValue);
    }
    
    public static StubbedLazyValue FromTaskFunc<T>(Func<Task<T>> func, T stubValue)
    {
        if (func is Func<Task<object>> fo)
            return new(fo, stubValue);
        else 
            return new((Func<Task<object>>)(async () => (object)await func()), stubValue);
    }
    
    public static StubbedLazyValue FromValueTaskFunc<T>(Func<ValueTask<T>> func, T stubValue)
    {
        if (func is Func<ValueTask<object>> fo)
            return new(fo, stubValue);
        else 
            return new((Func<Task<object>>)(async () => (object)await func()), stubValue);
    }
}
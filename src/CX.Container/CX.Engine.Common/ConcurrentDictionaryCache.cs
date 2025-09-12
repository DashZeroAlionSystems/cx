using System.Collections.Concurrent;
using JetBrains.Annotations;

namespace CX.Engine.Common;

public class ConcurrentDictionaryCache<TKey, TValue>
{
    public readonly ConcurrentDictionary<TKey, TValue> Dictionary;

    public Func<TKey, TValue> Factory;
    
    public ConcurrentDictionaryCache([NotNull] Func<TKey, TValue> factory, ConcurrentDictionary<TKey, TValue> dictionary = null)
    {
        Factory = factory ?? throw new ArgumentNullException(nameof(factory));
        Dictionary = dictionary ?? new();
    }

    public TValue GetOrAdd(TKey key) => Dictionary.GetOrAdd(key, Factory);
}
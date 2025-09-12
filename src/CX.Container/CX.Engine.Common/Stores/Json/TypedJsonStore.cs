using JetBrains.Annotations;

namespace CX.Engine.Common.Stores.Json;

public class TypedJsonStore<TValue>
{
    public readonly JsonStore JsonStore;
    
    public TypedJsonStore([NotNull] JsonStore jsonStore)
    {
        JsonStore = jsonStore ?? throw new ArgumentNullException(nameof(jsonStore));
    }
    
    public Task<TValue> GetAsync(string key) => JsonStore.GetAsync<TValue>(key);
    
    public Task SetAsync(string key, TValue value) => JsonStore.SetAsync(key, value);
    public Task SetIfNotExistsAsync(string key, TValue value) => JsonStore.SetIfNotExistsAsync(key, value);
    public Task DeleteAsync(string key) => JsonStore.DeleteAsync(key);
    public Task ClearAsync() => JsonStore.ClearAsync();
}
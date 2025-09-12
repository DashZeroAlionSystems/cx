namespace CX.Engine.Common.Stores.Json;

public interface IJsonStore
{
    Task DeleteAsync(string key);
    Task ClearAsync();
    Task<List<JsonStore.Row>> GetAllAsync();
    Task<T> GetAsync<T>(string key);
    Task<string> GetRawAsync(string key);
    Task SetAsync<T>(string key, T value);
    Task SetRawAsync(string key, string value);
    Task<bool> TryChangeAsync<T>(string key, T oldValue, T newValue);
    Task<bool> TryChangeAsync(string key, string oldValue, string value);
}

public interface IJsonStore<T> : IJsonStore
{
    Task<T> GetAsync(string key);
    Task SetAsync(string key, T value);
    Task<bool> TryChangeAsync(string key, T oldValue, T newValue);
}
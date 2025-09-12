namespace CX.Engine.Common.Stores.Binary;

public interface IBinaryStore
{
    Task DeleteAsync(string key);
    Task ClearAsync();
    Task<List<BinaryStoreRow>> GetAllAsync();
    Task<byte[]> GetBytesAsync(string key);
    Task<Stream> GetStreamAsync(string key);
    Task SetBytesAsync(string key, byte[] value);
    Task<bool> TryChangeAsync(string key, byte[] oldValue, byte[] value);    
}
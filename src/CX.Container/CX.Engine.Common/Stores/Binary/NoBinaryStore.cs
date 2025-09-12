namespace CX.Engine.Common.Stores.Binary;

public class NoBinaryStore : IBinaryStore
{
    public Task DeleteAsync(string key) => Task.CompletedTask;

    public Task ClearAsync() => Task.CompletedTask;

    public Task<List<BinaryStoreRow>> GetAllAsync() => Task.FromResult(new List<BinaryStoreRow>());

    public Task<byte[]> GetBytesAsync(string key) => Task.FromResult<byte[]>(null);

    public Task<Stream> GetStreamAsync(string key) => Task.FromResult<Stream>(null);

    public Task SetBytesAsync(string key, byte[] value) => Task.CompletedTask;

    public Task<bool> TryChangeAsync(string key, byte[] oldValue, byte[] value) => Task.FromResult(true);
}
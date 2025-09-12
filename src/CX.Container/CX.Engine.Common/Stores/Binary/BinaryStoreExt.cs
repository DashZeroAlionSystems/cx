using System.Text;

namespace CX.Engine.Common.Stores.Binary;

public static class BinaryStoreExt
{
    public static async Task<string> GetUtf8Async(this IBinaryStore store, string key)
    {
        var bytes = await store.GetBytesAsync(key);
        
        return bytes == null ? null : Encoding.UTF8.GetString(bytes);
    }
    
    public static async Task SetUtf8Async(this IBinaryStore store, string key, string value)
    {
        await store.SetBytesAsync(key, value == null ? null : Encoding.UTF8.GetBytes(value));
    }
}
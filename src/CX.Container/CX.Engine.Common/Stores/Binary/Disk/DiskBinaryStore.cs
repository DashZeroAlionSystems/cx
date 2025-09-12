using Microsoft.Extensions.Logging;

namespace CX.Engine.Common.Stores.Binary.Disk;

public sealed class DiskBinaryStore : IBinaryStore
{
    private readonly DiskBinaryStoreOptions _options;
    private readonly ILogger _logger;
    private readonly SemaphoreSlim _slimLock = new(1, 1);

    public DiskBinaryStore(DiskBinaryStoreOptions options, ILogger logger)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger;
        _options.Validate();
        _logger.LogInformation($"Initializing Disk Binary store with folder {_options.Folder}...");
        Directory.CreateDirectory(_options.Folder);
        _logger.LogInformation($"Initialized Disk Binary store with folder {_options.Folder}.");
    }

    private string GetPathForKey(string cacheKey) => Path.Combine(_options.Folder, Path.ChangeExtension(cacheKey, ".cache"));

    private void Delete_Internal(string key)
    {
        var path = GetPathForKey(key);
        if (File.Exists(path))
            File.Delete(path);
    }

    public async Task DeleteAsync(string key)
    {
        using var _ = await _slimLock.UseAsync();
        Delete_Internal(key);
    }

    public async Task ClearAsync()
    {
        using var _ = await _slimLock.UseAsync();

        Directory.Delete(_options.Folder, true);
        Directory.CreateDirectory(_options.Folder);
    }

    public async Task<List<BinaryStoreRow>> GetAllAsync()
    {
        using var _ = await _slimLock.UseAsync();

        var res = new List<BinaryStoreRow>();
        foreach (var file in Directory.GetFiles(_options.Folder, "*.*"))
            res.Add(new(Path.ChangeExtension(Path.GetFileName(file), "")[..^1], File.ReadAllBytes(file)));

        return res;
    }

    private async Task<byte[]> GetRaw_InternalAsync(string key)
    {
        var path = GetPathForKey(key);
        if (File.Exists(path))
            return await File.ReadAllBytesAsync(path);
        else
            return null;
    }

    public async Task<byte[]> GetBytesAsync(string key)
    {
        using var _ = await _slimLock.UseAsync();
        return await GetRaw_InternalAsync(key);
    }

    public async Task<Stream> GetStreamAsync(string key)
    {
        var bytes = await GetBytesAsync(key);
        return bytes != null ? new MemoryStream(bytes) : null;
    }

    private async Task SetRaw_InternalAsync(string key, byte[] value)
    {
        if (value == null)
            Delete_Internal(key);
        else
        {
            var path = GetPathForKey(key);
            await File.WriteAllBytesAsync(path, value);
        }
    }

    public async Task SetBytesAsync(string key, byte[] value)
    {
        using var _ = await _slimLock.UseAsync();
        await SetRaw_InternalAsync(key, value);
    }

    public async Task<bool> TryChangeAsync(string key, byte[] oldValue, byte[] value)
    {
        using var lock1 = await _slimLock.UseAsync();

        var curValue = await GetRaw_InternalAsync(key);

        if (curValue == null != (oldValue == null))
            return false;

        if (curValue == null || oldValue.AsSpan().SequenceEqual(curValue))
        {
            await SetRaw_InternalAsync(key, value);
            return true;
        }

        return false;
    }
}
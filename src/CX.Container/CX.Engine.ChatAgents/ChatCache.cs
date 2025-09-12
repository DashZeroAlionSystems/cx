using System.Text;
using System.Text.Json;
using CX.Engine.ChatAgents.OpenAI;
using CX.Engine.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CX.Engine.ChatAgents;

public class ChatCache
{
    private readonly IServiceProvider _sp;
    private readonly ILogger<ChatCache> _logger;
    public readonly ChatCacheOptions Options;
    public int CacheHits { get; private set; }
    public int CacheEntries => _cache.Count;

    private int _cacheDataVersion;
    private int _lastSavedCacheDataVersion;

    private const int Magic = 38120;
    private readonly Dictionary<string, ChatResponse> _cache = new();
    private readonly SemaphoreSlim _slimLock = new(1, 1);
    private readonly KeyedSemaphoreSlim _keyedLock = new();
    private readonly SemaphoreSlim _logLock = new(1, 1);
    

    public ChatCache(IOptions<ChatCacheOptions> options, IServiceProvider sp, ILogger<ChatCache> logger)
    {
        _sp = sp ?? throw new ArgumentNullException(nameof(sp));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        Options = options?.Value ?? throw new ArgumentNullException(nameof(options));

        Load();
    }

    private async Task<ChatResponse> GetFromCacheAsync(string cacheKey)
    {
        await _slimLock.WaitAsync();
        try
        {
            if (_cache.TryGetValue(cacheKey, out var response))
            {
                CacheHits++;
                return response;
            }

            return null;
        }
        finally
        {
            _slimLock.Release();
        }
    }

    public async Task<ChatResponse> ChatAsync(OpenAIChatRequest ctx, bool useCache = true)
    {
        if (ctx.Agent == null)
            throw new InvalidOperationException("Agent not set.");

        var agent = ctx.Agent;

        async ValueTask LogAsync(string cacheKey, ChatResponse answer)
        {
            if (string.IsNullOrWhiteSpace(Options.LogPath))
                return;

            using var _ = await _logLock.UseAsync();
            var sb = new StringBuilder();
            sb.Append("\n\n>>>----------------------------------------------\n\n");
            sb.Append(cacheKey);
            sb.Append("\n\n<<<----------------------------------------------\n\n");
            sb.Append(JsonSerializer.Serialize(answer));
            await File.AppendAllTextAsync(Options.LogPath, sb.ToString());
        }

        var cacheKey = ctx.GetCacheKey();

        if (!Options.UseCache || !useCache)
        {
            var res = await agent.RequestAsync(ctx);
            var answer = res.ToChatResponse(_logger, ((OpenAIChatAgent)agent).Options);

            await LogAsync(cacheKey, answer);

            return answer;
        }

        var response = await GetFromCacheAsync(cacheKey);
        if (response != null)
        {
            response.InflateFromRequest(ctx);

            if (ctx.Attachments.Count > 0)
                Thread.Sleep(1);

            await LogAsync(cacheKey, response);

            return response;
        }

        {
            using var _ = await _keyedLock.UseAsync(cacheKey);
            response = await GetFromCacheAsync(cacheKey);
            if (response != null)
            {
                response.InflateFromRequest(ctx);

                await LogAsync(cacheKey, response);

                return response;
            }

            var res = await agent.RequestAsync(ctx);
            response = res.ToChatResponse(_logger, ((OpenAIChatAgent)agent).Options);

            await _slimLock.WaitAsync();
            try
            {
                _cache[cacheKey] = response;
                Interlocked.Increment(ref _cacheDataVersion);
            }
            finally
            {
                _slimLock.Release();
            }

            await LogAsync(cacheKey, response);

            return response;
        }
    }

    public void Serialize(BinaryWriter bw)
    {
        _slimLock.Wait();
        try
        {
            bw.Write(Magic);
            bw.Write(2);
            bw.Write(_cache);
        }
        finally
        {
            _slimLock.Release();
        }
    }

    public void Deserialize(BinaryReader br)
    {
        var magic = br.ReadInt32();
        if (magic != Magic)
            throw new InvalidOperationException($"Invalid magic number (found {magic:#,##0} expected {Magic:#,##0}");

        var version = br.ReadInt32();
        if (version is < 1 or > 2)
            throw new InvalidOperationException($"Invalid version number (found {version:#,##0} expected 1 - 2)");

        var clc = new ChatLoadContext(br, version);

        _slimLock.Wait();
        try
        {
            _cache.Clear();
            _cache.PopulateFromReader(clc);
        }
        finally
        {
            _slimLock.Release();
        }
    }

    public void SaveToFile(string s)
    {
        var tmpFile = Path.ChangeExtension(s, ".tmp");
        var bakFile = Path.ChangeExtension(s, ".bak");

        {
            using var fs = new FileStream(tmpFile, FileMode.Create, FileAccess.Write, FileShare.None);
            using var bw = new BinaryWriter(fs);
            Serialize(bw);
        }

        if (File.Exists(s))
        {
            if (File.Exists(bakFile))
                File.Delete(bakFile);

            File.Move(s, bakFile);
        }

        File.Move(tmpFile, s!);
    }

    public void LoadFromFile(string s)
    {
        using var fs = new FileStream(s, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var br = new BinaryReader(fs);
        Deserialize(br);
    }

    public void Load()
    {
        if (!File.Exists(Options.CacheFile))
            return;

        LoadFromFile(Options.CacheFile);
    }

    /// <summary>
    /// Does nothing if no cache file set.
    /// Does nothing if no new changes to the cache detected.
    /// </summary>
    public bool Save()
    {
        if (string.IsNullOrWhiteSpace(Options.CacheFile))
            return false;

        if (_cacheDataVersion == _lastSavedCacheDataVersion)
            return false;

        _lastSavedCacheDataVersion = _cacheDataVersion;
        SaveToFile(Options.CacheFile);
        return true;
    }

    public void Clear()
    {
        _slimLock.Wait();
        try
        {
            _cache.Clear();
        }
        finally
        {
            _slimLock.Release();
        }
    }
}
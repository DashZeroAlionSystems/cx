using CX.Engine.Common.Embeddings.OpenAI;
using CX.Engine.Common.Tracing;
using Microsoft.Extensions.Options;

namespace CX.Engine.Common.Embeddings;

public class EmbeddingCache
{
    public readonly EmbeddingCacheOptions Options;
    public int CacheHits { get; private set; }
    public int CacheEntries => _cache.Count;

    private const int Magic = 313249127;
    private readonly Dictionary<EmbeddingKey, float[]> _cache = new();
    private readonly SemaphoreSlim SlimLock = new(1, 1);
    private readonly OpenAIEmbedder _embedder;
    private readonly KeyedSemaphoreSlim _keyedLock = new();
    private int _cacheVersion;
    private int _lastSavedCacheVersion;

    public EmbeddingCache(IOptions<EmbeddingCacheOptions> options, OpenAIEmbedder embedder)
    {
        Options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _embedder = embedder ?? throw new ArgumentNullException(nameof(embedder));
        Load();
    }

    private async Task<float[]> GetFromCacheAsync(EmbeddingKey key)
    {
        using var _ = await SlimLock.UseAsync();
        if (_cache.TryGetValue(key, out var embeddings))
        {
            CacheHits++;
            return embeddings;
        }

        return null;
    }

    public async Task<float[]> GetAsync(string model, string content)
    {
        OpenAIEmbedder.ThrowIfNotValidModel(model);

        return await CXTrace.Current.SpanFor(CXTrace.Section_GetEmbedding,
                new
                {
                    Model = model,
                    Content = content
                })
            .ExecuteAsync(async span =>
            {
                var key = new EmbeddingKey(model, content);

                if (Options.UseCache)
                {
                    var embeddings = await GetFromCacheAsync(key);
                    if (embeddings != null)
                    {
                        span.Output = new
                        {
                            VectorLength = embeddings.Length,
                            FromCache = true
                        };
                        return embeddings;
                    }
                }

                using var _ = await _keyedLock.UseAsync(content);

                if (Options.UseCache)
                {
                    var embeddings = await GetFromCacheAsync(key);
                    if (embeddings != null)
                    {
                        span.Output = new
                        {
                            VectorLength = embeddings.Length,
                            FromCache = true
                        };
                        return embeddings;
                    }
                }

                return await CXTrace.Current.GenerationFor(
                    CXTrace.Section_GenEmbedding,
                    model,
                    new(),
                    new
                    {
                        Content = content
                    }).ExecuteAsync(async gen =>
                {
                    {
                        var res = await _embedder.GetAsync(model, content);
                        var embeddings = res.Data[0].Embedding.ToArray();

                        if (Options.UseCache)
                        {
                            await SlimLock.WaitAsync();
                            try
                            {
                                _cache[key] = embeddings;
                                Interlocked.Increment(ref _cacheVersion);
                            }
                            finally
                            {
                                SlimLock.Release();
                            }
                        }

                        gen.CompletionTokens = 0;
                        gen.PromptTokens = res.Usage.PromptTokens;
                        gen.TotalTokens = res.Usage.TotalTokens;
                        gen.Output = new
                        {
                            VectorLength = embeddings.Length,
                            FromCache = false
                        };
                        span.Output = gen.Output;
                        return embeddings;
                    }
                });
            });
    }

    public void Serialize(BinaryWriter bw)
    {
        SlimLock.Wait();
        try
        {
            bw.Write(Magic);
            bw.Write(2);

            bw.Write7BitEncodedInt(_cache.Count);
            foreach (var kvp in _cache)
            {
                kvp.Key.Serialize(bw);
                bw.Write(kvp.Value);
            }
        }
        finally
        {
            SlimLock.Release();
        }
    }

    public async Task SerializeAsync(BinaryWriter bw)
    {
        using var _ = await SlimLock.UseAsync();
        bw.Write(Magic);
        bw.Write(2);

        bw.Write7BitEncodedInt(_cache.Count);
        foreach (var kvp in _cache)
        {
            kvp.Key.Serialize(bw);
            bw.Write(kvp.Value);
        }
    }

    public void Deserialize(BinaryReader br)
    {
        var magic = br.ReadInt32();
        if (magic != Magic)
            throw new InvalidOperationException($"Invalid magic number (found {magic:#,##0} expected {Magic:#,##0}");

        var version = br.ReadInt32();
        if (version != 1 && version != 2)
            throw new InvalidOperationException($"Invalid version number (found {version:#,##0} expected 1-2)");

        SlimLock.Wait();
        try
        {
            _cache.Clear();
            var cnt = br.Read7BitEncodedInt();
            for (var i = 0; i < cnt; i++)
            {
                EmbeddingKey key;

                if (version == 1)
                    key = new(OpenAIEmbedder.Models.text_embedding_3_large, br.ReadString());
                else
                    key = EmbeddingKey.FromBinaryReader(br);

                _cache[key] = br.ReadFloatArray();
            }
        }
        finally
        {
            SlimLock.Release();
        }
    }

    public async Task SaveToFileAsync(string s)
    {
        var tmpFile = Path.ChangeExtension(s, ".tmp");
        var bakFile = Path.ChangeExtension(s, ".bak");

        using (var fs = new FileStream(tmpFile, FileMode.Create, FileAccess.Write, FileShare.None))
        using (var bw = new BinaryWriter(fs))
            await SerializeAsync(bw);

        if (File.Exists(s))
        {
            if (File.Exists(bakFile))
                File.Delete(bakFile);

            File.Move(s, bakFile);
        }

        File.Move(tmpFile, s!);
    }

    public void SaveToFile(string s)
    {
        var tmpFile = Path.ChangeExtension(s, ".tmp");
        var bakFile = Path.ChangeExtension(s, ".bak");

        using (var fs = new FileStream(tmpFile, FileMode.Create, FileAccess.Write, FileShare.None))
        using (var bw = new BinaryWriter(fs))
            Serialize(bw);

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
        var bakFile = Path.ChangeExtension(s, ".bak");

        if (!File.Exists(s) && File.Exists(bakFile))
            s = bakFile;

        using var fs = new FileStream(s!, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var br = new BinaryReader(fs);

        Deserialize(br);
    }

    public void Load()
    {
        if (string.IsNullOrWhiteSpace(Options.CacheFile))
            return;

        if (!File.Exists(Options.CacheFile))
            return;

        LoadFromFile(Options.CacheFile);
    }

    /// <summary>
    /// Does nothing if no cache file specified.
    /// Does nothing if no changes in cache detected.
    /// </summary>
    public bool Save()
    {
        if (string.IsNullOrWhiteSpace(Options.CacheFile))
            return false;

        if (_lastSavedCacheVersion == _cacheVersion)
            return false;

        _lastSavedCacheVersion = _cacheVersion;
        SaveToFile(Options.CacheFile);
        return true;
    }

    public void Clear()
    {
        SlimLock.Wait();
        try
        {
            _cache.Clear();
        }
        finally
        {
            SlimLock.Release();
        }
    }
}
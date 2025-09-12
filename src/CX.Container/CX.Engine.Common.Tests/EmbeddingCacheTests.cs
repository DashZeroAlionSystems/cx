using CX.Engine.Common.Embeddings;
using CX.Engine.Common.Embeddings.OpenAI;
using CX.Engine.Common.Testing;
using CX.Engine.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CXLibTests;

public class EmbeddingCacheTests : TestBase
{
    private EmbeddingCache _embeddingCache = null!;

    protected override void ContextReady(IServiceProvider sp)
    {
        _embeddingCache = sp.GetRequiredService<EmbeddingCache>();
    }

    [Fact]
    public Task CacheTestAsync() => Builder.RunAsync(async () =>
    {
        var cacheFile = _embeddingCache.Options.CacheFile;

        try
        {
            _embeddingCache.Clear();
            Assert.Equal(0, _embeddingCache.CacheEntries);

            var content = "Cat";
            _ = await _embeddingCache.GetAsync(OpenAIEmbedder.Models.text_embedding_ada_002, content);
            Assert.Equal(0, _embeddingCache.CacheHits);
            Assert.Equal(1, _embeddingCache.CacheEntries);

            _ = await _embeddingCache.GetAsync(OpenAIEmbedder.Models.text_embedding_ada_002, content);
            Assert.Equal(1, _embeddingCache.CacheHits);

            var tmp = Path.GetTempPath();
            var tmpFile = Path.Combine(tmp, "embeddings.cache");
            _embeddingCache.Options.CacheFile = tmpFile;
            _embeddingCache.Save();

            _embeddingCache.Clear();
            Assert.Equal(0, _embeddingCache.CacheEntries);

            _embeddingCache.Load();
            Assert.Equal(1, _embeddingCache.CacheEntries);
        }
        finally
        {
            _embeddingCache.Options.CacheFile = cacheFile;
            _embeddingCache.Load();
        }
    });

    public EmbeddingCacheTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
        Builder.AddSecrets(SecretNames.EmbeddingCache.LocalDisk, SecretNames.OpenAIEmbedder);
        Builder.AddServices((sc, config) => sc.AddEmbeddings(config));
    }
}
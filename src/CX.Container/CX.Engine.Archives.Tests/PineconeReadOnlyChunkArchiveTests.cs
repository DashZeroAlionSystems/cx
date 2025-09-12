using CX.Engine.Common.Embeddings;
using CX.Engine.Common.PostgreSQL;
using CX.Engine.Common.Stores.Json;
using CX.Engine.Common.Testing;
using CX.Engine.Configuration;

namespace CX.Engine.Archives.Tests;

public class PineconeReadOnlyChunkArchiveTests : TestBase
{
    private PineconeReadOnlyChunkArchive _chunkArchive = null!;

    protected override void ContextReady(IServiceProvider sp)
    {
        _chunkArchive = sp.GetRequiredNamedService<PineconeReadOnlyChunkArchive>("vectormind-test-1536");
    }

    [Fact]
    public Task BasicTest() =>
        Builder.RunAsync(this,
            async () =>
            {
                await Assert.ThrowsAsync<NotSupportedException>(() => _chunkArchive.ImportAsync(new("a")));
                await Assert.ThrowsAsync<NotSupportedException>(() => _chunkArchive.ClearAsync());

                var res = await _chunkArchive.RetrieveAsync(("Can apples be red?", 0.5, 9_000));
                Assert.NotNull(res);
            });

    public PineconeReadOnlyChunkArchiveTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
        Builder
            .AddSecrets(
                SecretNames.Pinecone.vectormind_test_1536,
                SecretNames.EmbeddingCache.None,
                SecretNames.OpenAIEmbedder,
                SecretNames.PostgreSQL.pg_local,
                SecretNames.JsonStores.pg_local
                )
            .AddServices((sc, config) =>
            {
                sc.AddEmbeddings(config);
                sc.AddPostgreSQLClients(config);
                sc.AddJsonStores(config);
                sc.AddPinecone1Archives(config);
            });
    }
}
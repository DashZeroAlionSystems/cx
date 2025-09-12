using CX.Engine.Archives;
using CX.Engine.Archives.InMemory;
using CX.Engine.Common.Embeddings;
using CX.Engine.Common.Testing;
using CX.Engine.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CXLibTests;

public class InMemoryChunkArchiveTests : TestBase
{
    private EmbeddingCache _embeddingCache = null!;
    private InMemoryChunkArchive _inMemoryChunkArchive = null!;

    protected override void ContextReady(IServiceProvider sp)
    {
        _embeddingCache = sp.GetRequiredService<EmbeddingCache>();
        _inMemoryChunkArchive = sp.GetRequiredNamedService<InMemoryChunkArchive>("3-large");
    }

    [Fact]
    public Task AnimalTestAsync() => Builder.RunAsync(async () => 
    {
        await _inMemoryChunkArchive.ClearAsync();
        await _inMemoryChunkArchive.RegisterAsync(
            "Cat", "Dog", "Mouse", "Wolf", "Leopard", "Elephant", "Dove", "German Shepherd", "Pitbull", "Poodle");
        var matches = await _inMemoryChunkArchive.RetrieveAsync(("Poodle", 0.75, 3_000));
        _embeddingCache.Save();
        
        Assert.Equal("Poodle", matches[0].Chunk.Content);
    });

    [Fact]
    public Task InsecticideTestAsync() => Builder.RunAsync(async () =>
    {
        await _inMemoryChunkArchive.ClearAsync();
        await _inMemoryChunkArchive.RegisterAsync(
            "Abalex Insect Gel",
            "Acephate 750 SP",
            "Addition 150 SC",
            "Alpha-thrin 100 SC",
            "Alpha-Thrin Pest Kill",
            "Antset 200 SC",
            "Apex 500 WDG",
            "Arena 206 EC",
            "Biomectin 18 EC",
            "Buprofezin 500 WDG");

        var matches = await _inMemoryChunkArchive.RetrieveAsync(("Addition 180 SC", 0.75, 3_000));

        Assert.Equal("Addition 150 SC", matches[0].Chunk.Content);

        _ = await _inMemoryChunkArchive.RetrieveAsync(("Alpha-thrin", 0.75, 3_000));
        _embeddingCache.Save();
    });
    
    public InMemoryChunkArchiveTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
        Builder.AddSecrets(
            SecretNames.InMemoryArchives,
            SecretNames.EmbeddingCache.LocalDisk,
            SecretNames.OpenAIEmbedder);
        Builder.AddServices(static (sc, config) =>
        {
            sc.AddEmbeddings(config);
            sc.AddInMemoryArchives(config);
        });
    }
}
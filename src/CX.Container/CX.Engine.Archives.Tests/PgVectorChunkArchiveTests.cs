using CX.Engine.Archives.PgVector;
using CX.Engine.Common.Embeddings;
using CX.Engine.Common.PostgreSQL;
using CX.Engine.Common.Testing;
using CX.Engine.Configuration;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace CX.Engine.Archives.Tests;

public class PgVectorChunkArchiveTests : TestBase
{
    private EmbeddingCache _embeddingCache = null!;
    private PgVectorChunkArchive _chunkArchive = null!;

    protected override void ContextReady(IServiceProvider sp)
    {
        _embeddingCache = sp.GetRequiredService<EmbeddingCache>();
        _chunkArchive = sp.GetRequiredNamedService<PgVectorChunkArchive>("unit-test");
    }

    [Fact]
    public Task AnimalTestAsync() => Builder.RunAsync(async () =>
    {
        await _chunkArchive.ClearAsync();
        await _chunkArchive.RegisterAsync(
            "Cat", "Dog", "Mouse", "Wolf", "Leopard", "Elephant", "Dove", "German Shepherd", "Pitbull", "Poodle");
        var matches = await _chunkArchive.RetrieveAsync(("Poodle", 0.75, 3_000));
        _embeddingCache.Save();

        Assert.NotEmpty(matches);
        Assert.Equal("Poodle", matches[0].Chunk.Content);
    });

    [Fact]
    public Task DocumentTestAsync() => Builder.RunAsync(async () =>
    {
        await _chunkArchive.ClearAsync();
        var docAI = Guid.NewGuid();
        var docOO = Guid.NewGuid();
        await _chunkArchive.ImportAsync(docAI, ["AI is excellent.", "Large Language Models are relatively new technology."]);
        await _chunkArchive.ImportAsync(docOO, ["Object Orientated Programming is well established", "C# is a popular language."]);
        
        var matches = await _chunkArchive.RetrieveAsync(("AI", 0.1, 3_000));
        matches.Should().HaveCountGreaterThan(1);
        Assert.Equal(docAI, matches[0].Chunk.Metadata.DocumentId);
        Assert.Equal(docAI, matches[1].Chunk.Metadata.DocumentId);
        
        matches = await _chunkArchive.RetrieveAsync(("Object Orientated Programming", 0.1, 3_000));
        matches.Should().HaveCountGreaterThanOrEqualTo(1);
        Assert.Equal(docOO, matches[0].Chunk.Metadata.DocumentId);

        await _chunkArchive.RemoveDocumentAsync(docAI);
        
        matches = await _chunkArchive.RetrieveAsync(("AI", 0.1, 3_000));
        matches.Should().NotContain(r => r.Chunk.Metadata.DocumentId == docAI);
        matches.Should().Contain(r => r.Chunk.Metadata.DocumentId == docOO);
    });

    [Fact]
    public Task InsecticideTestAsync() => Builder.RunAsync(async () =>
    {
        await _chunkArchive.ClearAsync();
        await _chunkArchive.RegisterAsync(
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

        var matches = await _chunkArchive.RetrieveAsync(("Addition 180 SC", 0.75, 3_000));

        Assert.NotEmpty(matches);
        Assert.Equal("Addition 150 SC", matches[0].Chunk.Content);

        _ = await _chunkArchive.RetrieveAsync(("Alpha-thrin", 0.75, 3_000));
        _embeddingCache.Save();
    });
    
    
    public PgVectorChunkArchiveTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
        Builder.AddSecrets(
            SecretNames.PgVectorArchives,
            SecretNames.PostgreSQL.pg_local_vector,
            SecretNames.EmbeddingCache.LocalDisk,
            SecretNames.OpenAIEmbedder);
        Builder.AddServices(static (sc, config) =>
        {
            sc.AddEmbeddings(config);
            sc.AddPostgreSQLClients(config);
            sc.AddPgVectorArchives(config);
        });
    }
}
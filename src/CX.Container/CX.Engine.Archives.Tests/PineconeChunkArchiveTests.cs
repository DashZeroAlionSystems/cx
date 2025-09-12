using CX.Engine.Common.Embeddings;
using CX.Engine.Common.PostgreSQL;
using CX.Engine.Common.Stores.Json;
using CX.Engine.Common.Testing;
using CX.Engine.Configuration;
using CX.Engine.TextProcessors.Splitters;

namespace CX.Engine.Archives.Tests;

public class PineconeChunkArchiveTests : TestBase
{
    private PineconeChunkArchive _chunkArchive = null!;

    protected override void ContextReady(IServiceProvider sp)
    {
        _chunkArchive = sp.GetRequiredNamedService<PineconeChunkArchive>("vectormind-test-1536");
    }

    [Fact]
    public Task RemoveDocumentTest() => Builder.RunAsync(this,
        async () =>
        {
            await _chunkArchive.ClearAsync();
            var doc1Id = Guid.NewGuid();
            var doc2Id = Guid.NewGuid();
            var chunk1 = new TextChunk("Profession wise, Bob is a software engineer.");
            chunk1.Metadata.DocumentId = doc1Id;
            chunk1.SeqNo = 1;
            var chunk2 = new TextChunk("Hobby wise, Bob is a golfer.");
            chunk2.Metadata.DocumentId = doc2Id;
            chunk2.SeqNo = 1;
            await _chunkArchive.ImportAsync(doc1Id, [chunk1]);
            await _chunkArchive.ImportAsync(doc2Id, [chunk2]);

            var expected = 2;
            var tries = 1;

            while (true)
            {
                var res = await _chunkArchive.RetrieveAsync(("Tell me about Bob?", 0.5, 9_000));

                Assert.NotNull(res);

                //It takes Pinecone a few seconds to update it's index.
                if (res.Count != expected)
                {
                    await Task.Delay(100);
                    tries++;

                    if (tries == 10 && expected == 1)
                        Assert.Fail("Document did not delete: 10 tries reached");
                    continue;
                }

                if (expected == 2)
                {
                    await _chunkArchive.RemoveDocumentAsync(doc1Id);
                    tries = 1;
                    expected = 1;
                    continue;
                }

                Assert.Equal(chunk2.Metadata.DocumentId, res[0].Chunk.Metadata.DocumentId);
                break;
            }
        });

    [Fact]
    public Task BasicTest() =>
        Builder.RunAsync(this,
            async () =>
            {
                await _chunkArchive.ClearAsync();

                while (true)
                {
                    var res = await _chunkArchive.RetrieveAsync(("What profession does Bob work in?", 0, 9_000));

                    Assert.NotNull(res);

                    //It takes Pinecone a few seconds to update it's index.
                    if (res.Count > 0)
                    {
                        await Task.Delay(100);
                        continue;
                    }

                    break;
                }

                var chunk = new TextChunk("Profession wise, Bob is a software engineer.");
                chunk.Metadata.DocumentId = Guid.NewGuid();
                chunk.Metadata.GetAttachments(true)!.Add(new()
                {
                    FileName = "resume.pdf",
                    FileUrl = "http://bob.com/resume.pdf",
                    Description = "Bob's resume",
                });
                await _chunkArchive.ImportAsync(chunk);

                while (true)
                {
                    var res = await _chunkArchive.RetrieveAsync(("What profession does Bob work in?", 0.5, 9_000));

                    Assert.NotNull(res);

                    //It takes Pinecone a few seconds to update it's index.
                    if (res.Count == 0)
                    {
                        await Task.Delay(100);
                        continue;
                    }

                    Assert.Single(res);
                    Assert.NotNull(res[0]);
                    var r = res[0].Chunk;
                    Assert.NotNull(r);
                    Assert.Equal(chunk.Content, r.Content);
                    Assert.NotNull(r.Metadata);
                    var att = r.Metadata.GetAttachments(false);
                    Assert.NotNull(att);
                    Assert.Single(att);
                    Assert.Equal("resume.pdf", att[0].FileName);
                    Assert.Equal("http://bob.com/resume.pdf", att[0].FileUrl);
                    Assert.Equal("Bob's resume", att[0].Description);
                    break;
                }
            });

    public PineconeChunkArchiveTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
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
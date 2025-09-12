using CX.Engine.Common.Embeddings;
using CX.Engine.Common.Embeddings.OpenAI;
using CX.Engine.Common.Testing;
using CX.Engine.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CXLibTests;

public class OpenAIEmbedderTests : TestBase
{
    private OpenAIEmbedder _openAIEmbedder = null!;

    protected override void ContextReady(IServiceProvider sp)
    {
        _openAIEmbedder = sp.GetRequiredService<OpenAIEmbedder>();
    }

    [Fact]
    public Task EmbeddingRequestAsync() => Builder.RunAsync(async () =>
    {
        var res = await _openAIEmbedder.GetAsync(OpenAIEmbedder.Models.text_embedding_ada_002, "Cat");

        Assert.NotNull(res);
        Assert.Equal("list", res.Object);
        Assert.Equal(OpenAIEmbedder.Models.text_embedding_ada_002, res.Model, StringComparer.InvariantCultureIgnoreCase);
        Assert.Equal("embedding", res.Data[0].Object);
        Assert.Equal(0, res.Data[0].Index);
        Assert.Equal(1536, res.Data[0].Embedding.Count);
        Assert.Single(res.Data);
    });

    public OpenAIEmbedderTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
        Builder.AddSecrets(SecretNames.OpenAIEmbedder);
        Builder.AddServices((sc, config) =>
        {
            sc.AddEmbeddings(config);
        });
    }
}
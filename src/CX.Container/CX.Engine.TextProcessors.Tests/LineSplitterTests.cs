using CX.Engine.Common.Testing;
using CX.Engine.Configuration;
using CX.Engine.TextProcessors.Splitters;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace CXLibTests;

public class LineSplitterTests : TestBase
{
    private LineSplitter _lineSplitter = null!;

    protected override void ContextReady(IServiceProvider sp)
    {
        _lineSplitter = sp.GetRequiredService<LineSplitter>();
    }

    [Fact]
    public Task LineContinuationTests() => Builder.RunAsync(async () =>
    {
        var content = @"Your tractor may be equipped with an optional engine ex-
haust brake. The exhaust brake holds compression in the
engine, which slows crankshaft rotation and thereby re-
duces vehicle speed.";

        var chunks = await _lineSplitter.ChunkAsync(new(content));
        Assert.Equal(
            "Your tractor may be equipped with an optional engine exhaust brake. " +
            "The exhaust brake holds compression in the\r\nengine, which slows crankshaft " +
            "rotation and thereby reduces vehicle speed.",
            chunks[0].Content);
    });

    public LineSplitterTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
        Builder.AddSecrets(SecretNames.LineSplitter._400);
        Builder.AddServices((sc, config) => { sc.AddLineSplitter(config); });
    }
}
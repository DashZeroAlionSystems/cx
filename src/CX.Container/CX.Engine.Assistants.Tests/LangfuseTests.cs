using CX.Engine.Common;
using CX.Engine.Common.Testing;
using CX.Engine.Common.Tracing.Langfuse;
using CX.Engine.Common.Tracing.Langfuse.Events;
using CX.Engine.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace CXLibTests;

public class LangfuseTests : TestBase
{
    private LangfuseService _langfuse = null!;

    protected override void ContextReady(IServiceProvider sp)
    {
        _langfuse = sp.GetRequiredService<LangfuseService>();
    }

    [Fact]
    public Task SessionTest() => Builder.RunAsync(async () =>
    {
        var now = DateTime.UtcNow.AddSeconds(-60);

        var tr = new CreateOrUpdateTraceEvent
            {
                UserId = "unit-test",
                Tags = ["question"],
                Input = "What is the meaning of life?",
                Name = "What is the meaning of life?".Preview(50)
            }
            .AssignNewSessionId()
            .AssignNewTraceId();

        tr.Enqueue(_langfuse, now);

        var tr2 = new CreateOrUpdateTraceEvent
        {
            TraceId = tr.TraceId,
            Output = "44"
        };

        tr2.Enqueue(_langfuse, now);

        var spEmbed = new CreateSpanEvent
            {
                TraceId = tr.TraceId,
                Name = "embed",
                Input = tr.Input,
                StatusMessage = "Started",
                Version = "1"
            }.AssignNewSpanId();
        
        spEmbed.Enqueue(_langfuse, now);
        now = now.AddSeconds(3);

        var spEmbedEnd = new UpdateSpanEvent
        {
            TraceId = tr.TraceId,
            SpanId = spEmbed.SpanId,
            StatusMessage = "Success",
            Version = "1",
            Output = new double[] { 1, 2, 3 },
            End = true
        };
            spEmbedEnd.Enqueue(_langfuse, now);

        var gen = new CreateGenerationEvent
        {
            TraceId = tr.TraceId,
            GenId = "ge-" + Guid.NewGuid(),
            Input = tr.Input,
            Model = "gpt-4-turbo",
            Name = "What is the meaning of life?".Preview(50)
        };
            gen.Enqueue(_langfuse, now);
        now = now.AddSeconds(5);
        var genEnd = new UpdateGenerationEvent
        {
            TraceId = tr.TraceId,
            GenId = gen.GenId,
            End = true,
            Output = "44",
            PromptTokens = 100,
            CompletionTokens = 5,
            TotalTokens = 105,
        };
            genEnd.Enqueue(_langfuse, now);

        await tr;
        await tr2;
        await spEmbed;
        await spEmbedEnd;
        await gen;
        await genEnd;
    });

    public LangfuseTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
        Builder.AddSecrets(SecretNames.Langfuse.Local);
        Builder.AddServices((sc, config) => sc.AddLangfuse(config));
    }
}
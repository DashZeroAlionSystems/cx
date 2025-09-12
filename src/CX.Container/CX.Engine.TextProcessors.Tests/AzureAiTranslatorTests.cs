using System.Text;
using CX.Engine.Common;
using CX.Engine.Common.Testing;
using CX.Engine.Configuration;
using Xunit.Abstractions;

namespace CX.Engine.TextProcessors.Tests;

public class AzureAiTranslatorTests : TestBase
{
    private AzureAITranslator _translatorEn = null!;
    private AzureAITranslator _translatorAf = null!;

    protected override void ContextReady(IServiceProvider sp)
    {
        _translatorEn = sp.GetRequiredNamedService<AzureAITranslator>("en");
        _translatorAf = sp.GetRequiredNamedService<AzureAITranslator>("af");
    }

    [Fact]
    public Task TranslatesToEnglishTest() => Builder.RunAsync(this,
        async () =>
        {
            var res = await _translatorEn.ProcessAsync("Goeie more vanaf Suid Afrika!");

            Assert.Equal("Good morning from South Africa!", res);
        });

    [Fact]
    public Task TranslatesToAfrikaansTest() => Builder.RunAsync(this,
        async () =>
        {
            var res = await _translatorAf.ProcessAsync("Good morning from London!");

            Assert.Equal("Goeie more uit Londen!", res);
        });

    [Fact]
    public Task TranslateLongTest() => Builder.RunAsync(this,
        async () =>
        {
            var segment = "Die groen bal hop teen die wit muur.";
            var sb = new StringBuilder();
            for (var i = 0; i < 300_000 / segment.Length; i++)
                sb.AppendLine(segment);

            var res = await _translatorEn.ProcessAsync(sb.ToString().Trim());
            Assert.NotNull(res);
        });

    public AzureAiTranslatorTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
        Builder.AddConfig(SecretsProvider.Get(SecretNames.AzureAITranslators));
        Builder.AddServices((sc, config) => sc.AddTextProcessors(config));
    }
}
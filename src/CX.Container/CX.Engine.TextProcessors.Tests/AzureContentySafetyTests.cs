using CX.Engine.Common;
using CX.Engine.Common.Testing;
using CX.Engine.Configuration;
using Xunit.Abstractions;

namespace CX.Engine.TextProcessors.Tests;

public class AzureContentySafetyTests : TestBase
{
    private AzureContentSafety _contentSafety = null!;
    
    protected override void ContextReady(IServiceProvider sp)
    {
        _contentSafety = sp.GetRequiredNamedService<AzureContentSafety>("safe");
    }

    [Fact]
    public Task TestSequence() => Builder.RunAsync(this, async () => {
        //Does not throw
        await _contentSafety.ProcessAsync("This is harmless text");

        //Blocks Level 4 self-harm text
        var ex = await Assert.ThrowsAsync<ContentSafetyException>(() => _contentSafety.ProcessAsync("Go kill yourself, you are worthless."));
        Assert.Equal(4, ex.Level);
        Assert.Equal(AzureContentSafety.CategorySelfHarm, ex.Category);
    });

    public AzureContentySafetyTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
        Builder.AddConfig(SecretsProvider.Get(SecretNames.AzureContentSafety));
        Builder.AddServices((sc, config) => sc.AddTextProcessors(config));
    }
}
using CX.Engine.Common;
using CX.Engine.Common.Stores.Binary;
using CX.Engine.Common.Testing;
using CX.Engine.Configuration;
using CX.Engine.DocExtractors;
using CX.Engine.DocExtractors.Text;
using CXLibTests.Resources;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace CXLibTests;

public class MSDocAnalyzerTests : TestBase
{
    private MSDocAnalyzer _msDocAnalyzer = null!;

    protected override void ContextReady(IServiceProvider sp)
    {
        _msDocAnalyzer = sp.GetRequiredService<MSDocAnalyzer>();
    }

    [Fact]
    public Task BasicTest() => Builder.RunAsync(async () => 
    {
        var res = await _msDocAnalyzer.ExtractToTextAsync(this.GetResource(Resource.This_is_a_test_pdf), new());
        
        Assert.NotNull(res);
        Assert.StartsWith("--- PAGE 1 ---\r\nThis is a test", res);
    });

    public MSDocAnalyzerTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
        Builder.AddSecrets(SecretNames.MSDocAnalyzer, SecretNames.DiskBinaryStores.Common);
        Builder.AddServices((sc, config) =>
        {
            sc.AddBinaryStores(config);
            sc.AddMSDocAnalyzer(config);
        });
    }
}
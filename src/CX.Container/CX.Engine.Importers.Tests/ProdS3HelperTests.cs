using CX.Engine.Common;
using CX.Engine.Common.Testing;
using CX.Engine.Configuration;
using CX.Engine.Importing.Prod;

namespace CX.Engine.Importing.Tests;

public class ProdS3HelperTests : TestBase
{
    private ProdS3Helper _helper = null!;

    [Fact]
    public Task CanGetFileAsync() => Builder.RunAsync(async () =>
    {
        var file = await _helper.GetObjectAsync(_helper.Options.PublicBucket, "d10f4992bf9517e4e1cb53315c1707a5.pdf");
        Assert.NotNull(file);
    });

    public ProdS3HelperTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
        Builder.AddSecrets(SecretNames.ProdS3Helpers.prods3helper_playground);
        Builder.AddServices((sc, config) => sc.AddProdS3Helpers(config));
    }

    protected override void ContextReady(IServiceProvider sp)
    {
        base.ContextReady(sp);
        _helper = sp.GetRequiredNamedService<ProdS3Helper>("prods3helper_playground");
    }
}
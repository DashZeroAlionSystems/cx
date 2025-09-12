using CX.Engine.Common.PostgreSQL;
using CX.Engine.Common.Stores;
using CX.Engine.Common.Stores.Binary;
using CX.Engine.Common.Stores.Binary.Disk;
using CX.Engine.Common.Testing;
using CX.Engine.Configuration;

namespace CX.Engine.Common.Tests;

public class DiskBinaryStoreTests : TestBase
{
    private IBinaryStore _store = null!;

    [Fact]
    public async Task RawTests() => await Builder.RunAsync(async () =>
    {
        var b1 = "abc"u8.ToArray();
        var b2 = "def"u8.ToArray();
        
        await _store.ClearAsync();
        Assert.Empty(await _store.GetAllAsync());
        await _store.TryChangeAsync("a", null, b1);
        Assert.Equal(b1, await _store.GetBytesAsync("a"));
        var all = await _store.GetAllAsync();
        Assert.Single(all);
        Assert.Equal("a", all[0].Key);
        Assert.Equal(b1, all[0].Value);

        Assert.False(await _store.TryChangeAsync("a", null, b1));
        Assert.True(await _store.TryChangeAsync("a", b1, b2));
        Assert.Equal(b2, await _store.GetBytesAsync("a"));

        await _store.DeleteAsync("b");
        Assert.Null(await _store.GetBytesAsync("b"));
        
        await _store.SetUtf8Async("c", "hello");
        Assert.Equal("hello", await _store.GetUtf8Async("c"));
    });

    protected override void ContextReady(IServiceProvider sp)
    {
        _store = sp.GetRequiredNamedService<IBinaryStore>("disk.unit_test");
        Assert.IsType<DiskBinaryStore>(_store);
    }

    public DiskBinaryStoreTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
        Builder.AddConfig(SecretsProvider.Get(SecretNames.DiskBinaryStores.Common));
        Builder.AddServices(static (sc, config) =>
        {
            sc.AddPostgreSQLClients(config);
            sc.AddStores(config);
        });
    }
}
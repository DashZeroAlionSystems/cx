using CX.Engine.Common.PostgreSQL;
using CX.Engine.Common.Stores;
using CX.Engine.Common.Stores.Json;
using CX.Engine.Common.Testing;
using CX.Engine.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CX.Engine.Common.Tests;

public class Crc32JsonStoreTests : TestBase
{
    private PostgreSQLClient _pgLocal;
    private Crc32JsonStore _store;

    [Fact]
    public async Task Basics() => await Builder.RunAsync(async () =>
    {
        var storeId = (_pgLocal, "cache_crc32_unit_tests");
        await _store.ClearAsync(storeId);
        Assert.Null(await _store.GetRawAsync(storeId, "not_found"));
        Assert.Null(await _store.GetAsync<string>(storeId, "test_string"));
        await _store.SetAsync(storeId, "test_string", "test_value");
        Assert.Equal("test_value", await _store.GetAsync<string>(storeId, "test_string"));
        await _store.RemoveAsync(storeId, "test_string");
        Assert.Null(await _store.GetAsync<string>(storeId, "test_string"));
    });

    public Crc32JsonStoreTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
        Builder.AddConfig(SecretsProvider.Get(SecretNames.PostgreSQL.pg_local));
        Builder.AddServices(static (sc, config) =>
        {
            sc.AddPostgreSQLClients(config);
            sc.AddStores(config);
        });
    }

    protected override void ContextReady(IServiceProvider sp)
    {
        _pgLocal = sp.GetRequiredNamedService<PostgreSQLClient>("pg_local");
        _store = sp.GetRequiredService<Crc32JsonStore>();
    }
}
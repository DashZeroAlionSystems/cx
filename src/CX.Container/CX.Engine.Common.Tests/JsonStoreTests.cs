using CX.Engine.Common.PostgreSQL;
using CX.Engine.Common.Stores.Json;
using CX.Engine.Common.Testing;
using CX.Engine.Configuration;

namespace CX.Engine.Common.Tests;

public class JsonStoreTests : TestBase
{
    private IJsonStore _jsonStore = null!;

    [Fact]
    public async Task RawTests() => await Builder.RunAsync(async () =>
    {
        await _jsonStore.ClearAsync();
        Assert.Empty(await _jsonStore.GetAllAsync());
        await _jsonStore.TryChangeAsync("a", null, "[1]");
        Assert.Equal("[1]", await _jsonStore.GetRawAsync("a"));
        var all = await _jsonStore.GetAllAsync();
        Assert.Single(all);
        Assert.Equal("a", all[0].Key);
        Assert.Equal("[1]", all[0].Value);

        Assert.False(await _jsonStore.TryChangeAsync("a", null, "[1]"));
        Assert.True(await _jsonStore.TryChangeAsync("a", "[1]", "[2]"));
        Assert.Equal("[2]", await _jsonStore.GetRawAsync("a"));

        await _jsonStore.SetRawAsync("b", "{}");
        await _jsonStore.SetRawAsync("b", "[]");
        Assert.Equal("[]", await _jsonStore.GetRawAsync("b"));

        await _jsonStore.DeleteAsync("b");
        Assert.Null(await _jsonStore.GetRawAsync("b"));
    });

    [Fact]
    public async Task TypedTests() => await Builder.RunAsync(async () =>
    {
        await _jsonStore.SetAsync("a", 1);
        Assert.Equal(1, await _jsonStore.GetAsync<int>("a"));

        await _jsonStore.SetAsync("a", "bob");
        Assert.Equal("bob", await _jsonStore.GetAsync<string>("a"));

        await _jsonStore.SetAsync("a", new[] { 1, 2, 3 });
        Assert.True((await _jsonStore.GetAsync<int[]>("a"))?.SequenceEqual(new[] { 1, 2, 3 }));

        await _jsonStore.SetAsync<int?>("a", null);
        Assert.Null(await _jsonStore.GetAsync<int[]>("a"));

        await _jsonStore.SetAsync("a", new List<int> { 1, 2, 3 });
        Assert.True((await _jsonStore.GetAsync<List<int>>("a"))?.SequenceEqual(new[] { 1, 2, 3 }));

        Assert.False(await _jsonStore.TryChangeAsync("a", [0, 1, 2], new List<int> { 4, 5, 6 }));
        Assert.True((await _jsonStore.GetAsync<List<int>>("a"))?.SequenceEqual(new[] { 1, 2, 3 }));

        Assert.True(await _jsonStore.TryChangeAsync("a", [1, 2, 3], new List<int> { 4, 5, 6 }));
        Assert.True((await _jsonStore.GetAsync<List<int>>("a"))?.SequenceEqual(new[] { 4, 5, 6 }));
    });

    protected override void ContextReady(IServiceProvider sp)
    {
        _jsonStore = sp.GetRequiredNamedService<IJsonStore>("vector-tracker");
    }

    public JsonStoreTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
        Builder.AddConfig(SecretsProvider.Get(SecretNames.PostgreSQL.pg_local),
            SecretsProvider.Get(SecretNames.JsonStores.pg_local)
        );
        Builder.AddServices(static (sc, config) =>
        {
            sc.AddPostgreSQLClients(config);
            sc.AddJsonStores(config);
        });
    }
}
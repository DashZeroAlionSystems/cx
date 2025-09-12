using CX.Engine.Common.Formatting;

namespace CX.Engine.Common.Tests;

public class StubbedLazyDictionaryTests
{
    [Fact]
    public async Task BasicTests()
    {
        StubbedLazyDictionary sld = new () {
            ["a"] = 1
        };
        sld.SetToStubMode();
        Assert.Equal(1, sld["a"]);

        sld = new()
        {
            ["a"] = StubbedLazyValue.FromValue(1)
        };
        sld.SetToStubMode();
        Assert.Equal(1, sld["a"]);

        sld = new()
        {
            ["a"] = StubbedLazyValue.FromFunc(() => 2,  1)
        };
        sld.SetToStubMode();
        Assert.Equal(1, sld["a"]);
        await sld.ResolveAsync();
        Assert.Equal(2, sld["a"]);

        sld = new()
        {
            ["a"] = StubbedLazyValue.FromTaskFunc(() => Task.FromResult(2), 1)
        };
        sld.SetToStubMode();
        Assert.Equal(1, sld["a"]);
        await sld.ResolveAsync();
        Assert.Equal(2, sld["a"]);
        
        sld = new()
        {
            ["a"] = StubbedLazyValue.FromValueTaskFunc(() => ValueTask.FromResult(2), 1)
        };
        sld.SetToStubMode();
        Assert.Equal(1, sld["a"]);
        await sld.ResolveAsync();
        Assert.Equal(2, sld["a"]);
    }

    [Fact]
    public async Task NestedTests()
    {
        var seen = false;
        
        var sld = new StubbedLazyDictionary()
        {
            ["x"] = new StubbedLazyDictionary()
            {
                ["y"] = StubbedLazyValue.FromFunc(() => 2, 1),
                ["z"] = StubbedLazyValue.FromFunc(() =>
                {
                    seen = true;
                    return 3;
                }, 1)
            }
        };
        sld.SetToStubMode();
        var x = (IDictionary<string, object>)sld["x"];
        Assert.Equal(1, x["y"]);
        await sld.ResolveAsync();
        Assert.Equal(2, x["y"]);

        Assert.False(seen);
    }
}
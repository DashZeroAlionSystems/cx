using CX.Engine.Common.Formatting;

namespace CX.Engine.Common.Tests;

public class CxSmartTests
{
    [Fact]
    public void BasicTests()
    {
        Assert.Null(CxSmart.Format(null));
    }

    [Fact]
    public async Task LazyFormatTests()
    {
        var seen = false;
        Assert.Null(await CxSmart.LazyFormatAsync(null));
        var res = await CxSmart.LazyFormatAsync("{x.y}", new StubbedLazyDictionary()
        {
            ["x"] = new StubbedLazyDictionary()
            {
                ["y"] = StubbedLazyValue.FromFunc(() => 2, 1),
                ["z"] = StubbedLazyValue.FromFunc(
                    () =>
                    {
                        seen = true;
                        return 3;
                    },
                    1)
            }
        });
        Assert.Equal("2", res);
        Assert.False(seen);
    }
}
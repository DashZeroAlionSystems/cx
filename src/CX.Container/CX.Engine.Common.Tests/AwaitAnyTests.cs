namespace CX.Engine.Common.Tests;

public class AwaitAnyTests
{
    [Fact]
    public async Task Basics()
    {
        Assert.Equal(1, await MiscHelpers.AwaitAnyAsync(1));
        Assert.Equal(1, await MiscHelpers.AwaitAnyAsync(Task.FromResult(1)));
        Assert.Equal(1, await MiscHelpers.AwaitAnyAsync(ValueTask.FromResult(1)));
    }
}
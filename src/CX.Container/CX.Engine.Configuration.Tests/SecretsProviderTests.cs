using CX.Engine.Common;

namespace CX.Engine.Configuration.Tests;

public class SecretsProviderTests
{
    [Fact]
    public void SecretsProviderBasics()
    {
        Assert.Equal("Hi, world!", SecretsProvider.Get("hiworld.txt"));
    }
}
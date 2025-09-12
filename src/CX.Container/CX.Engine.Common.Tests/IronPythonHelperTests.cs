#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
using System.Text.Json.Nodes;
using CX.Engine.Common.IronPython;
using FluentAssertions;

namespace CX.Engine.Common.Tests;

public static class IronPythonHelperTests
{
    [Fact]
    public static async Task HelloWorldTest()
    {
        JsonNode node = await IronPythonExecutor.ExecuteScriptAsync("JsonValue.Create('Hello, World!')");
        ((JsonNode)node)!.GetValue<string>().Should().Be("Hello, World!");
    }

    [Fact]
    private static async Task ArrayResolveTest()
    {
        List<string> res = ["a", "b"];
        await IronPythonHelper.ResolveArrayAsync(res);
        res.Should().BeEquivalentTo(new List<string> { "a", "b" });

        res = ["a", "ironpython:'b'", "z"];
        await IronPythonHelper.ResolveArrayAsync(res);
        res.Should().BeEquivalentTo(new List<string> { "a", "b", "z" });

        res = ["a", "ironpython:['b', 'c']", "z"];
        await IronPythonHelper.ResolveArrayAsync(res);
        res.Should().BeEquivalentTo(new List<string> { "a", "b", "c", "z" });

        res = ["a", "ironpython:['b', 'c']", "z"];
        await IronPythonHelper.ResolveArrayAsync(res);
        res.Should().BeEquivalentTo(new List<string> { "a", "b", "c", "z" });

        async Task<List<string>> GetStringsAsync() => ["b", "c"];
        var ctx = new IronPythonContext().AddMethods(GetStringsAsync);
        res =
        [
            "a", """
                 ironpython:
                 def on_done(x):
                   x.Add('d')
                   return x

                 cs.ChainResult(GetStringsAsync(), on_done)
                 """,
            "z"
        ];
        await IronPythonHelper.ResolveArrayAsync(res, ctx);
        res.Should().BeEquivalentTo(new List<string> { "a", "b", "c", "d", "z" });

    }
}
using System.Text.Json.Nodes;
using CX.Engine.Common.Json;
using FluentAssertions;

namespace CX.Engine.Common.Testing;

public class JsonEvalTests
{
    [Fact]
    public async Task EscapeWithinArray()
    {
        var jo = JsonNode.Parse(
        """
            [
               "/ironpython:"  
            ]
            """);

        await new JsonEval().EvalAsync(jo);
        var ja = jo as JsonArray;
        Assert.NotNull(ja);
        ja.Single().GetValue<string>().Should().Be("ironpython");
    }

    [Fact]
    public async Task IronPythonHelloWorld()
    {
        var jo = JsonNode.Parse(
            """
            {
               "message": "ironpython:'Hello, World!'"  
            }
            """);
        
        await new JsonEval().EvalAsync(jo);
        Assert.NotNull(jo);
        Assert.NotNull(jo["message"]);
        jo["message"].GetValue<string>().Should().Be("Hello, World!");
    }
}
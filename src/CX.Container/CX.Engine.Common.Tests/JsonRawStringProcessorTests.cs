using System.Text.Json;
using CX.Engine.Common.Json;

namespace CX.Engine.Common.Tests;

public class JsonRawStringProcessorTests
{
    [Fact]
    public void Test()
    {
        // Arrange
        var input = """"
                    {
                      "a": 
                    """
                    This
                    is
                    a
                    single
                    line
                    """
                    }
                    """";

        // Act
        var result = JsonRawStringProcessor.NormalizeTripleQuotedStrings(input);

        var doc = JsonDocument.Parse(result);
        var actual = doc.RootElement.GetProperty("a").GetString();
        Assert.Equal("This\r\nis\r\na\r\nsingle\r\nline", actual);
    }
}
using System.Text.Json;
using CX.Engine.Common.Meta;
using FluentAssertions;

namespace CX.Engine.Common.Tests;

public class MetaTests
{
    [Fact]
    public void SerializationTests()
    {
        var meta = new DocumentMeta
        {
            Id = Guid.NewGuid(),
            SourceDocument = "file.txt",
            Description = "Hi there!",
            Pages = 10,
            ContainsTables = true,
            Tags = [ "junk" ],
            Attachments = [ new() { FileName = "abc.txt" } ]
        };

        var json = JsonSerializer.Serialize(meta);

        var deserialized = JsonSerializer.Deserialize<DocumentMeta>(json);

        deserialized.Should().NotBeNull();

        deserialized!.Id.Should().Be(meta.Id);
        deserialized.SourceDocument.Should().Be("file.txt");
        deserialized.Description.Should().Be("Hi there!");
        deserialized.Pages.Should().Be(10);
        deserialized.ContainsTables.Should().Be(true);
        deserialized.Tags.Should().NotBeNull();
        deserialized.Tags.Should().SatisfyRespectively(
            tag => tag.Should().Be("junk")
        );
        deserialized.Attachments.Should().NotBeNull();
        deserialized.Attachments.Should().SatisfyRespectively(
            att => att.FileName.Should().Be("abc.txt")
        );

    }
}
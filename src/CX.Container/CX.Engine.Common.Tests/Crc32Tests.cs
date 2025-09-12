using FluentAssertions;

namespace CX.Engine.Common.Tests;

public class Crc32Tests
{
    [Fact]
    public void Crc32Basics()
    {
        var crcA = "A".GetCrc32();
        var crcB = "B".GetCrc32();
        crcA.Should().NotBe(crcB);
        var crca = "a".GetCrc32();
        crcA.Should().NotBe(crca);
        var crcA2 = "A".GetCrc32();
        crcA.Should().Be(crcA2);
    }
}
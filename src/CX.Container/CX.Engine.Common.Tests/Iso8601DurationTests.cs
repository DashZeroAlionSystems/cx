using FluentAssertions;

namespace CX.Engine.Common.Tests;

public class Iso8601DurationTests
{
    [Fact]
    public void Iso8601DurationsTest()
    {
        MiscHelpers.ParseIso8601Timespan("PT5S").Should().Be(TimeSpan.FromSeconds(5));
        MiscHelpers.ParseIso8601Timespan("PT3M").Should().Be(TimeSpan.FromMinutes(3));
        MiscHelpers.ParseIso8601Timespan("PT2H").Should().Be(TimeSpan.FromHours(2));
        MiscHelpers.ParseIso8601Timespan("P1D").Should().Be(TimeSpan.FromDays(1));
        MiscHelpers.ParseIso8601Timespan("P2W").Should().Be(TimeSpan.FromDays(14));

        MiscHelpers.ParseIso8601Timespan("P1DT12H").Should().Be(TimeSpan.FromDays(1) + TimeSpan.FromHours(12));
        MiscHelpers.ParseIso8601Timespan("P3DT4H30M").Should().Be(TimeSpan.FromDays(3) + TimeSpan.FromHours(4) + TimeSpan.FromMinutes(30));
        MiscHelpers.ParseIso8601Timespan("PT36H").Should().Be(TimeSpan.FromHours(36)); // 1 day 12 hours
        MiscHelpers.ParseIso8601Timespan("P2DT3H4M5.678S").Should().Be(new(2, 3, 4, 5, 678));

        MiscHelpers.ParseIso8601Timespan("PT0.5S").Should().Be(TimeSpan.FromMilliseconds(500));
        MiscHelpers.ParseIso8601Timespan("PT1.75M").Should().Be(TimeSpan.FromMinutes(1) + TimeSpan.FromSeconds(45));
        MiscHelpers.ParseIso8601Timespan("PT2.5H").Should().Be(TimeSpan.FromHours(2) + TimeSpan.FromMinutes(30));
    }
}
using CX.Engine.Common.Numbering;
using CX.Engine.Common.Xml;

namespace CX.Engine.Common.Tests;

public class OrderedListSequenceTests
{
    [Fact]
    public void NumericTests()
    {
        var seq = new NumericOrderedListSequence();
        seq.Next();
        Assert.Equal("1", seq.Current);
        seq.Next();
        Assert.Equal("2", seq.Current);
    }

    [Fact]
    public void RomanNumeralTests()
    {
        var seq = new RomanNumeralOrderedListSequence();
        seq.Next();
        Assert.Equal("i", seq.Current);
        seq.Next();
        Assert.Equal("ii", seq.Current);
    }
    
    [Fact]
    public void AlphabeticalTests()
    {
        var seq = new AlphabeticOrderedListSequence();
        seq.Next();
        Assert.Equal("a", seq.Current);
        seq.Next();
        Assert.Equal("b", seq.Current);
    }

}
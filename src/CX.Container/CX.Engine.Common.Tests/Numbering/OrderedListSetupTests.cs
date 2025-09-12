using CX.Engine.Common.Numbering;
using static CX.Engine.Common.Numbering.OrderedListSequenceType;

namespace CX.Engine.Common.Tests;

public class OrderedListSetupTests
{
    [Fact]
    public void BasicTests()
    {
        var root = new OrderedListSetup(
            (Numeric, "."), 
            (Alphabetic, "."), 
            (Roman, ")"));
        root.GetSetupForLevel(1);
        
        Assert.Equal("1. ", root.Next());
        Assert.Equal("2. ", root.Next());
        var level2 = root.Nest();
        Assert.Equal("2.a. ", level2.Next());
        Assert.Equal("2.b. ", level2.Next());
        var level3 = level2.Nest();
        Assert.Equal("2.b.i) ", level3.Next());
        Assert.Equal("2.b.ii) ", level3.Next());
        Assert.Equal("3. ", root.Next());
        Assert.Equal("2.c. ", level2.Next());
        Assert.Equal("2.b.iii) ", level3.Next());
    }
}
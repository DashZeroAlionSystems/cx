using CX.Engine.Common.CodeProcessing;

namespace CX.Engine.Common.Tests.CodeProcessing;

public class AccessorTests
{
    [Fact]
    public void ParseTests()
    {
        {
            var ast = (PropertyAccessNode)Accessor.Parse("a.b");
            Assert.False(ast.LeftIsOptional);
            Assert.Equal("a", ((IdentifierNode)ast.Left).Id);
            Assert.Equal("b", ((IdentifierNode)ast.Right).Id);
        }

        {
            var ast = (PropertyAccessNode)Accessor.Parse(".b", "a");
            Assert.False(ast.LeftIsOptional);
            Assert.Equal("a", ((IdentifierNode)ast.Left).Id);
            Assert.Equal("b", ((IdentifierNode)ast.Right).Id);
        }
        
        {
            var ast = (PropertyAccessNode)Accessor.Parse("a?.b", "a");
            Assert.True(ast.LeftIsOptional);
            Assert.Equal("a", ((IdentifierNode)ast.Left).Id);
            Assert.Equal("b", ((IdentifierNode)ast.Right).Id);
        }

        {
            var ast = (PropertyAccessNode)Accessor.Parse("?.b", "a");
            Assert.True(ast.LeftIsOptional);
            Assert.Equal("a", ((IdentifierNode)ast.Left).Id);
            Assert.Equal("b", ((IdentifierNode)ast.Right).Id);
        }

        {
            var ast = (PropertyAccessNode)Accessor.Parse("a.b.c");
            var left = (PropertyAccessNode)ast.Left;
            Assert.Equal("a", ((IdentifierNode)left.Left).Id);
            Assert.Equal("b", ((IdentifierNode)left.Right).Id);
            Assert.Equal("c", ((IdentifierNode)ast.Right).Id);
        }

        {
            var ast = (PropertyAccessNode)Accessor.Parse(".b.c", "a");
            var left = (PropertyAccessNode)ast.Left;
            Assert.Equal("a", ((IdentifierNode)left.Left).Id);
            Assert.Equal("b", ((IdentifierNode)left.Right).Id);
            Assert.Equal("c", ((IdentifierNode)ast.Right).Id);
        }

        {
            var ast = (ArrayAccessNode)Accessor.Parse("a[1]");
            Assert.Equal("a", ((IdentifierNode)ast.Left).Id);
            Assert.Equal(1, ((ASTConstantNode)ast.Index).Constant.IntegerValue);
        }

        {
            var ast = (ArrayAccessNode)Accessor.Parse("a?[1]");
            Assert.True(ast.LeftIsOptional);
            Assert.False(ast.IndexIsOptional);
            Assert.Equal("a", ((IdentifierNode)ast.Left).Id);
            Assert.Equal(1, ((ASTConstantNode)ast.Index).Constant.IntegerValue);
        }

        {
            var ast = (ArrayAccessNode)Accessor.Parse("a[?1]");
            Assert.False(ast.LeftIsOptional);
            Assert.True(ast.IndexIsOptional);
            Assert.Equal("a", ((IdentifierNode)ast.Left).Id);
            Assert.Equal(1, ((ASTConstantNode)ast.Index).Constant.IntegerValue);
        }

        {
            var ast = (ArrayAccessNode)Accessor.Parse("[1]", "a");
            Assert.Equal("a", ((IdentifierNode)ast.Left).Id);
            Assert.Equal(1, ((ASTConstantNode)ast.Index).Constant.IntegerValue);
        }

        {
            var ast = (PropertyAccessNode)Accessor.Parse("a[1].b");
            var left = (ArrayAccessNode)ast.Left;
            Assert.Equal("a", ((IdentifierNode)left.Left).Id);
            Assert.Equal(1, ((ASTConstantNode)left.Index).Constant.IntegerValue);
            Assert.Equal("b", ((IdentifierNode)ast.Right).Id);
        }
        
        {
            var ast = (PropertyAccessNode)Accessor.Parse("[1].b", "a");
            var left = (ArrayAccessNode)ast.Left;
            Assert.Equal("a", ((IdentifierNode)left.Left).Id);
            Assert.Equal(1, ((ASTConstantNode)left.Index).Constant.IntegerValue);
            Assert.Equal("b", ((IdentifierNode)ast.Right).Id);
        }

    }

    [Fact]
    public async Task EvalTests()
    {
        Assert.Equal(3, await Accessor.EvaluateAsync("x", 3, "x"));
        Assert.Equal(5, await Accessor.EvaluateAsync("x.y", new { y = 5 }, "x"));
        Assert.Equal(5, await Accessor.EvaluateAsync(".y", new { y = 5 }, "x"));
        Assert.Equal(5, await Accessor.EvaluateAsync("x.y", new Dictionary<string, object> { ["y"] = 5 }, "x"));
        Assert.Equal("a", await Accessor.EvaluateAsync("arr[0]", new[] { "a" }, "arr"));
        Assert.Equal("a", await Accessor.EvaluateAsync("[0]", new[] { "a" }, "arr"));
    }

}
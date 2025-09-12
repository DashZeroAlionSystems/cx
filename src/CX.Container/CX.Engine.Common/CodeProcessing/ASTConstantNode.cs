namespace CX.Engine.Common.CodeProcessing;

public class ASTConstantNode : ASTNode
{
    public Token Constant;
    
    public override bool IsOptional => false;

    public ASTConstantNode()
    {
    }

    public ASTConstantNode(Token constant)
    {
        Constant = constant;
    }
}
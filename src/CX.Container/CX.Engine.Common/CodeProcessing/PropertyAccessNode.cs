namespace CX.Engine.Common.CodeProcessing;

public class PropertyAccessNode : ASTNode
{
    public ASTNode Left;
    public ASTNode Right;
    public bool LeftIsOptional;

    public override bool IsOptional => LeftIsOptional;

    public PropertyAccessNode()
    {
    }

    public PropertyAccessNode(ASTNode left, ASTNode right, bool leftIsOptional = false)
    {
        Left = left;
        Right = right;
        LeftIsOptional = leftIsOptional;
    }
}
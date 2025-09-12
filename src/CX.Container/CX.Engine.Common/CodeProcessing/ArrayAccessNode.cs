namespace CX.Engine.Common.CodeProcessing;

public class ArrayAccessNode : ASTNode
{
    public ASTNode Left;
    public ASTNode Index;
    public bool LeftIsOptional;
    public bool IndexIsOptional;
    
    public override bool IsOptional => LeftIsOptional && IndexIsOptional;

    public ArrayAccessNode()
    {
    }

    public ArrayAccessNode(ASTNode left, ASTNode index, bool leftIsOptional = false, bool indexIsOptional = false)
    {
        Left = left;
        Index = index;
        LeftIsOptional = leftIsOptional;
        IndexIsOptional = indexIsOptional;
    }
}
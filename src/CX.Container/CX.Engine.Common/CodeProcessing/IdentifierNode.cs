namespace CX.Engine.Common.CodeProcessing;

public class IdentifierNode : ASTNode
{
    public string Id;
    
    public override bool IsOptional => false;

    public IdentifierNode()
    {
    }

    public IdentifierNode(string id)
    {
        Id = id;
    }
}
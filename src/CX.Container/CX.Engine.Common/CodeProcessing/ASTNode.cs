namespace CX.Engine.Common.CodeProcessing;

public abstract class ASTNode 
{
    public abstract bool IsOptional { get; }

    public string Path;
}
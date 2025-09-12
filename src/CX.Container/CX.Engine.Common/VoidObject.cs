namespace CX.Engine.Common;

public class VoidObject
{
    public override string ToString() => "void";

    private VoidObject()
    {
    }

    public static readonly VoidObject Instance = new();
}
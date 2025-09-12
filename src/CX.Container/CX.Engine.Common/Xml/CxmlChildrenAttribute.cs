namespace CX.Engine.Common.Xml;

[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Class)]
public class CxmlChildrenAttribute : Attribute
{
    public bool All;
    public bool UnknownAsStrings;
    
    public CxmlChildrenAttribute(bool all = false, bool unknownAsStrings = false)
    {
        All = all;
        UnknownAsStrings = unknownAsStrings;
    }
}
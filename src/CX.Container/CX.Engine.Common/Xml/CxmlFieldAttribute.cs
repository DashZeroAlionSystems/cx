namespace CX.Engine.Common.Xml;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class CxmlFieldAttribute : Attribute
{
    public string Name { get; set; }

    public CxmlFieldAttribute()
    {
    }
    
    public CxmlFieldAttribute(string name)
    {
        Name = name;
    }
}
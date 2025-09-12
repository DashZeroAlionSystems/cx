namespace CX.Engine.Common.Xml;

[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Field | AttributeTargets.Property)]
public class CxmlContentAttribute : Attribute
{
    public bool Trim { get; set; } = true;
}
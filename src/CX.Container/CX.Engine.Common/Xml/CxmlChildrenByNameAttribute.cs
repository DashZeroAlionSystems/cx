using JetBrains.Annotations;

namespace CX.Engine.Common.Xml;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class CxmlChildrenByNameAttribute : Attribute
{
    public string Name;
    public bool References;
    
    public CxmlChildrenByNameAttribute([NotNull] string name)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
    }
}
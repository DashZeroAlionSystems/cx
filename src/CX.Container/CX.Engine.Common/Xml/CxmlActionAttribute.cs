using JetBrains.Annotations;

namespace CX.Engine.Common.Formatting;

[UsedImplicitly]
[AttributeUsage(AttributeTargets.Method)]
public class CxmlActionAttribute : Attribute
{
    public string Name;
    
    public CxmlActionAttribute([NotNull] string name)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
    }
}
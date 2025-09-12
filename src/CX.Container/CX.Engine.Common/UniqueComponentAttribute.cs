namespace CX.Engine.Common;

/// <summary>
/// Indicates that only one of these components should be present on an object.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class UniqueComponentAttribute : Attribute
{
    
}
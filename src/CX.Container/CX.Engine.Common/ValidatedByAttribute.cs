namespace CX.Engine.Common;

[AttributeUsage(AttributeTargets.Class)]
public class ValidatedByAttribute : Attribute
{
    public readonly Type ValidationType;
    
    public ValidatedByAttribute(Type validationType)
    {
        ValidationType = validationType;
    }
}
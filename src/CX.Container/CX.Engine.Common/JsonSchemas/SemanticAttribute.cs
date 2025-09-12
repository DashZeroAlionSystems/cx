namespace CX.Engine.Common.JsonSchemas;

public class SemanticAttribute : Attribute
{
    public string Description { get; }
    public string[] Choices;
    public TypeDefinition[] AnyOf;
    
    public SemanticAttribute(string description = null, string[] choices = null, Type[] anyOf = null)
    {
        Description = description;
        Choices = choices;
        AnyOf = anyOf?.Select(t => new TypeDefinition(new(t))).ToArray();
    }
}
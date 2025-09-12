namespace CX.Engine.Common.JsonSchemas;

[AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
public class SemanticNoteAttribute : Attribute
{
    public string Note;
    
    public SemanticNoteAttribute(string note)
    {
        Note = note;
    }
}
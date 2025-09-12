namespace CX.Engine.TextProcessors;

public class ContentSafetyException : TextValidationException
{
    public readonly int Level;
    public readonly string Category;
    
    public ContentSafetyException(string category, int level) : base ($"level {level} {category} content")
    {
        Category = category ?? throw new ArgumentNullException(nameof(category));
        
        if (level < 1)
            throw new ArgumentOutOfRangeException(nameof(level), level, "Level must be greater than or equal to 1.");
        
        Level = level;
    }
}
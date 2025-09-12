namespace CX.Engine.TextProcessors;

public abstract class TextValidationException : Exception
{
    protected TextValidationException(string invalidContentDescription) : base(invalidContentDescription)
    {
    }
}
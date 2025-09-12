namespace CX.Engine.Common.Formatting;

public class TagOverflowException : Exception
{
    public TagOverflowException(string message) : base(message)
    {
    }
}
namespace CX.Engine.Common.CodeProcessing;

public class InternalParserException : Exception
{
    public InternalParserException(string message) : base(message)
    {
    }
    
    public static void Throw(string message) => throw new InternalParserException(message);
}
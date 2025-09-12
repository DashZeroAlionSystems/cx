namespace CX.Engine.Common.CodeProcessing;

public class ParserException : Exception
{
    public int Index;
    
    public ParserException(string message) : base(message)
    {
    }

    public ParserException(string message, int index): base(message)
    {
        Index = index;
    }
    
    public static ParserException Throw(string message) => throw new ParserException(message);
    public static ParserException Throw(string message, int index) => throw new ParserException(message, index);
    public static ParserException Throw(int index, string expected, string found) => throw new ParserException($"Expected {expected} but found {found} at position {index}", index);
    public static ParserException Throw(int index, TokenType expected, TokenType found) => throw new ParserException($"Expected {expected} but found {found} at position {index}", index);
    public static ParserException Throw(int index, TokenType[] expected, TokenType found) => throw new ParserException($"Expected {expected.ToString(", ", " or ")} but found {found} at position {index}", index);
}
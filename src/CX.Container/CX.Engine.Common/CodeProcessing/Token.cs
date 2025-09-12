namespace CX.Engine.Common.CodeProcessing;

public struct Token
{
    public TokenType TokenType;
    public string StringValue;
    public int IntegerValue;
    public int Index;
    
    public static Token ForIdentifier(string id, int index = -1) => new() { TokenType = TokenType.Identifier, StringValue = id, Index = index };
    public static Token ForDot(int index = -1) => new() { TokenType = TokenType.Dot, Index = index };
    public static Token ForArrayStart(int index = -1) => new() { TokenType = TokenType.ArrayStart, Index = index };
    public static Token ForArrayEnd(int index = -1) => new() { TokenType = TokenType.ArrayEnd, Index = index };
    public static Token ForInteger(int i, int index = -1) => new() { TokenType = TokenType.Integer, IntegerValue = i, Index = index};
    public static Token ForEof(int index = -1) => new() { TokenType = TokenType.Eof, Index = index };
    public static Token ForInvalid(int index = -1) => new() { TokenType = TokenType.Invalid, Index = index };
    public static Token ForQuestionMark(int index = -1) => new() { TokenType = TokenType.QuestionMark, Index = index};
    
    public static implicit operator TokenType(Token token) => token.TokenType;
}
using static CX.Engine.Common.CodeProcessing.TokenType;

namespace CX.Engine.Common.CodeProcessing;

public class CodeTokenizer
{
    public readonly Stack<int> IndexStack = new();
    public string Path;
    public int Index;

    public CodeTokenizer(string path)
    {
        Path = path ?? throw new ArgumentNullException(nameof(path));
        Index = 0;
    }
    
    public bool CurrentCharIsEof => Index >= Path.Length;
    
    public char CurrentChar => Path[Index];
    
    public bool CurrentCharIsDigit => char.IsDigit(CurrentChar);
    public bool CurrentCharIsDot => CurrentChar == '.';
    public bool CurrentCharIsArrayStart => CurrentChar == '[';
    public bool CurrentCharIsArrayEnd => CurrentChar == ']';
    public bool CurrentCharIsUnderscore => CurrentChar == '_';
    public bool CurrentCharIsQuestionMark => CurrentChar == '?';
    
    public char NextChar => Index + 1 < Path.Length ? Path[Index + 1] : '\0';

    public bool CurrentCharIsIdentifierStart => char.IsLetter(CurrentChar) || CurrentCharIsUnderscore; 

    /// <summary>
    /// Identifiers are letters, digits and underscores.  Identifiers cannot start with a digit.
    /// </summary>
    /// <returns>Returns null if no identifier read, otherwise returns the identifier</returns>
    public string TryReadIdentifier()
    {
        if (CurrentCharIsEof || CurrentCharIsDigit)
            return null;
        
        var start = Index;
        while (Index < Path.Length && (char.IsLetterOrDigit(Path[Index]) || Path[Index] == '_'))
            Index++;

        if (Index == start)
            return null;
        
        return Path.Substring(start, Index - start);
    }
    
    public bool TryReadDot()
    {
        if (CurrentCharIsDot)
        {
            Index++;
            return true;
        }

        return false;
    }

    public int? TryReadInt()
    {
        if (CurrentCharIsEof || !CurrentCharIsDigit)
            return null;
        
        var start = Index;
        while (Index < Path.Length && char.IsDigit(Path[Index]))
            Index++;

        return int.Parse(Path.Substring(start, Index - start));
    }

    public bool TryReadQuestionMark()
    {
        if (CurrentCharIsEof)
            return false;
        
        if (CurrentCharIsQuestionMark)
        {
            Index++;
            return true;
        }

        return false;
    }

    public void ReadQuestionMark()
    {
        if (!TryReadQuestionMark())
            ParserException.Throw(Index, QuestionMark, ReadToken());
    }

    public bool TryReadArrayStart()
    {
        if (CurrentCharIsArrayStart)
        {
            Index++;
            return true;
        }

        return false;
    }
    
    public bool TryReadArrayEnd()
    {
        if (CurrentCharIsArrayEnd)
        {
            Index++;
            return true;
        }

        return false;
    }

    public void PushIndex()
    {
        IndexStack.Push(Index);
    }
    
    public void PopIndex()
    {
        Index = IndexStack.Pop();
    }

    public Token ReadToken()
    {
        if (CurrentCharIsEof)
            return Token.ForEof(Index);
        
        if (CurrentCharIsDot)
        {
            Index++;
            return Token.ForDot(Index - 1);
        }

        if (CurrentCharIsArrayStart)
        {
            Index++;
            return Token.ForArrayStart(Index - 1);
        }

        if (CurrentCharIsArrayEnd)
        {
            Index++;
            return Token.ForArrayEnd(Index - 1);
        }

        if (CurrentCharIsQuestionMark)
        {
            Index++;
            return Token.ForQuestionMark(Index - 1);
        }

        var idx = Index;
        if (CurrentCharIsDigit)
        {
            var i = TryReadInt();
            if (i.HasValue)
                return Token.ForInteger(i.Value, idx);
        }

        {
            var id = TryReadIdentifier();
            if (id != null)
                return Token.ForIdentifier(id, idx);
        }

        return Token.ForInvalid(idx);
    }

    public string ReadIdentifier()
    {
        if (CurrentCharIsEof)
            ParserException.Throw(Index, Identifier, Eof);

        if (!CurrentCharIsIdentifierStart)
            ParserException.Throw(Index, Identifier, ReadToken());

        return TryReadIdentifier() ?? throw new InternalParserException("identifier == null");
    }

    public int ReadInteger()
    {
        if (CurrentCharIsEof)
            ParserException.Throw(Index, Integer, Eof);

        if (!CurrentCharIsDigit)
            ParserException.Throw(Index, Integer, ReadToken());

        return TryReadInt() ?? throw new InternalParserException("int == null");
    }

    public void ReadArrayEnd()
    {
        if (CurrentCharIsEof)
            ParserException.Throw(Index, ArrayEnd, Eof);

        if (!CurrentCharIsArrayEnd)
            ParserException.Throw(Index, ArrayEnd, ReadToken());

        Index++;
    }

    public Token PreviewToken()
    {
        PushIndex();
        var idx = Index;
        var res = ReadToken();
        res.Index = idx;
        PopIndex();
        return res;
    }
}
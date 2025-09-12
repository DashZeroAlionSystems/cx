using System.Text;
using CX.Engine.Common.Numbering;
using CX.Engine.Common.Xml;

namespace CX.Engine.Common.Rendering;

public class TextRenderContext
{
    public static AsyncLocal<TextRenderContext> AsyncLocalCurrent { get; } = new();
    
    public static TextRenderContext Current
    {
        get => AsyncLocalCurrent.Value ??= new();
        set => AsyncLocalCurrent.Value = value ?? new();
    }

    public CxmlScope Scope;
    public OrderedListSetup OlSetup;
    public TextRenderContext Parent;
    public List<StringBuilder> StringBuilders = [];
    
    public StringBuilder Sb => StringBuilders.Peek();

    public static void Push(StringBuilder sb) => Current.StringBuilders.Push(sb);
    
    public static void EnsureStartsOnNewLine(int openLines = 0)
    {
        Current.StringBuilders.EnsureStartsOnNewLine(openLines);
    }

    public static TextRenderContext InheritOrNew(StringBuilder sb = null)
    {
        var cur = Current;
        TextRenderContext newCtx;
        
        if (AsyncLocalCurrent.Value == null)
        {
            newCtx = new();
            newCtx.StringBuilders.Push(sb);
            Current = newCtx;
        }

        newCtx = new() { 
            Parent = cur,
            OlSetup = cur.OlSetup,
            Scope = cur.Scope?.Inherit(),
            StringBuilders = [..cur.StringBuilders]
        };

        if (sb != null)
            newCtx.StringBuilders.Push(sb);
        
        return Current = newCtx;
    }
}
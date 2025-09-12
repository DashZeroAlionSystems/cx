using System.Text;
using CX.Engine.Common;
using CX.Engine.Common.Rendering;
using CX.Engine.Common.Xml;

namespace CX.Engine.Assistants.AssessmentBuilder.Xml;

[CxmlChildren(All = true, UnknownAsStrings = true)]
public class AssessmentInstructions : ICxmlAddChild, IRenderToText, ICxmlChildren
{
    public string Title;
    
    public List<object> CxmlChildren { get; } = [];
    public async Task AddChildAsync(object o)
    {
        CxmlChildren.Add(o);
    }

    public async Task RenderToTextAsync()
    {
        var ctx = TextRenderContext.Current;
        var sb = ctx.Sb;
        if (!string.IsNullOrWhiteSpace(Title))
        {
            TextRenderContext.EnsureStartsOnNewLine(1);
            sb.Append(Title);
        }

        TextRenderContext.EnsureStartsOnNewLine(1);
        await CxmlChildren.RenderToTextContextAsync();
        TextRenderContext.EnsureStartsOnNewLine(1);
    }

    IEnumerable<object> ICxmlChildren.CxmlChildren() => CxmlChildren;
}
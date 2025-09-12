using System.Text;
using CX.Engine.Common;
using CX.Engine.Common.Rendering;
using CX.Engine.Common.Xml;
using JetBrains.Annotations;

namespace CX.Engine.Assistants.AssessmentBuilder.Xml;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
[CxmlChildren(All = true, UnknownAsStrings = true)]
public class AssessmentPaper : ICxmlAddChild, IRenderToText, ICxmlChildren, ICxmlPromptScope
{
    public List<object> CxmlChildren { get; } = [];
    public List<PromptSectionNode> PromptSections { get; set; } = [];

    public int TotalMarks { get; set; }

    public string Subject { get; set; }

    public int Grade { get; set; }

    public string Time { get; set; }
    public string PaperType { get; set; }
    public string FocusArea { get; set; }

    public IEnumerable<AssessmentSection> Sections => this.DescendantsOfType<AssessmentSection>();

    public async Task AddChildAsync(object o)
    { 
        if (o is PromptSectionNode psn)
            PromptSections.Add(psn);
        else
            CxmlChildren.Add(o);
    }

    public async Task RenderToTextAsync()
    {
        var ctx = TextRenderContext.InheritOrNew();

        var sb = new StringBuilder();
        ctx.StringBuilders.Push(sb);
        await CxmlChildren.RenderToTextContextAsync();
        
        TextRenderContext.EnsureStartsOnNewLine(1);
        sb.AppendLine($"GRAND TOTAL: {TotalMarks:#,##0}");
        ctx.Parent.Sb.Append(sb.ToString().Trim());
    }

    IEnumerable<object> ICxmlChildren.CxmlChildren() => CxmlChildren;
}
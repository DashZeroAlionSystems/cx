using CX.Engine.Common.Rendering;
using CX.Engine.Common.Xml;
using JetBrains.Annotations;

namespace CX.Engine.Assistants.AssessmentBuilder.Xml;

[CxmlChildren(All = true, UnknownAsStrings = true)]
[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class AssessmentSection : ICxmlAddChild, IRenderToText, ICxmlChildren, ICxmlHasParentProp, ICxmlDependencyScope
{
    public object Parent { get; set; }
    public string Name;
    public string Title;
    public string Time;
    public int TotalMarks;
    public List<object> CxmlChildren { get; } = [];
    IEnumerable<object> ICxmlChildren.CxmlChildren() => CxmlChildren;
    public List<object> DependsOn { get; set; }

    public async Task AddChildAsync(object o) => CxmlChildren.Add(o);

    public async Task RenderToTextAsync()
    {
        var ctx = TextRenderContext.Current;
        var sb = ctx.Sb;
        var hasId = !string.IsNullOrWhiteSpace(Name);
        var hasTitle = !string.IsNullOrWhiteSpace(Title);
        
        if (hasId && hasTitle)
            sb.AppendLine($"SECTION {Name}: {Title}".ToUpperInvariant());
        else if (hasId)
            sb.AppendLine($"SECTION {Name}".ToUpperInvariant());
        else if (hasTitle)
            sb.AppendLine($"SECTION {Title}".ToUpperInvariant());
        else
            sb.AppendLine("SECTION");

        await CxmlChildren.RenderToTextContextAsync();

        TextRenderContext.EnsureStartsOnNewLine(1);

        sb.AppendLine($"TOTAL SECTION {Name}: {TotalMarks:#,##0}");
        sb.AppendLine();
    }

}
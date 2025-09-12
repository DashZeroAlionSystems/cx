using CX.Engine.Common.Rendering;
using JetBrains.Annotations;

namespace CX.Engine.Common.Xml;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
[CxmlChildren(All = true, UnknownAsStrings = true)]
public class Paragraph : ICxmlAddChild, IRenderToText, ICxmlChildren, ICxmlId
{
    public List<object> CxmlChildren { get; } = [];

    public async Task AddChildAsync(object o) => CxmlChildren.Add(o);

    public async Task RenderToTextAsync()
    {
        TextRenderContext.EnsureStartsOnNewLine(1);
        await CxmlChildren.RenderToTextContextAsync();
        TextRenderContext.EnsureStartsOnNewLine();
    }

    IEnumerable<object> ICxmlChildren.CxmlChildren() => CxmlChildren;
    public string Id { get; set; }
}
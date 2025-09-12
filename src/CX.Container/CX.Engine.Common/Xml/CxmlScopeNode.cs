using CX.Engine.Common.Rendering;

namespace CX.Engine.Common.Xml;

public class CxmlScopeNode : ICxmlDependencyScope, ICxmlChildren, ICxmlAddChild, IRenderToText, ICxmlPromptScope, ICxmlHasParentProp
{
    public List<object> DependsOn { get; set; }
    public List<object> Children { get; set; } = [];
    public List<PromptSectionNode> PromptSections { get; set; } = [];
    public object Parent { get; set; }

    public IEnumerable<object> CxmlChildren() => Children.Union(PromptSections);

    public Task AddChildAsync(object o)
    {
        if (o is PromptSectionNode psn)
            PromptSections.Add(psn);
        else
            Children.Add(o);
        return Task.CompletedTask;
    }

    public Task RenderToTextAsync() => Children.RenderToTextContextAsync();
}
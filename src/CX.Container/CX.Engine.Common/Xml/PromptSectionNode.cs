using Cx.Engine.Common.PromptBuilders;
using CX.Engine.Common.Rendering;

namespace CX.Engine.Common.Xml;

public class PromptSectionNode : IRenderToText, ICxmlChildren
{
    public int? Order;
    
    [CxmlChildren(true, true)]
    public List<object> Children = [];

    [CxmlField("node-id")]
    public object Node { get; set; }

    public async Task RenderToTextAsync()
    {
        var scope = TextRenderContext.Current.Scope;
        if (Node != null)
        {
            var node = await scope.ResolveReferenceFieldValueAsync(Node);
            if (node != null)
                await node.RenderToTextContextAsync();
        }

        await Children.RenderToTextContextAsync();
    }

    public IEnumerable<object> CxmlChildren() => Children;
}

public static class PromptSectionNodeExt
{
    public static async Task AddAsync(this PromptBuilder pb, IEnumerable<PromptSectionNode> sections, CxmlScope scope = null)
    {
        foreach (var section in sections)
        {
            var s = await section.RenderToStringAsync(scope, smartFormat: true);
            pb.Add(s, section.Order);
        }
    }
}
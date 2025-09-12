using System.Collections;
using CX.Engine.Common.Rendering;
using HtmlAgilityPack;

namespace CX.Engine.Common.Xml;

public class Foreach : IRenderToText
{
    public string Var;
    public object In;
    [CxmlContent] public List<HtmlNode> Item;

    public async Task RenderToTextAsync()
    {
        if (!(In is IEnumerable enumerable))
            return;

        if (Item == null)
            return;

        var ctx = TextRenderContext.InheritOrNew();
        var sb = ctx.Sb;
        
        if (ctx.Scope == null)
            throw new InvalidOperationException($"{nameof(ctx)}.{nameof(ctx.Scope)} is null");

        var varName = Var;
        if (string.IsNullOrWhiteSpace(Var))
            varName = "item";

        foreach (var item in enumerable)
        {
            var scope = ctx.Scope.Inherit();
            scope.Context[varName] = item;
            foreach (var node in Item)
                sb.Append(await Cxml.EvalStringAsync(node, scope));
        }
    }
}
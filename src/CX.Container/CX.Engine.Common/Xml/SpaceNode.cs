using CX.Engine.Common.Rendering;

namespace CX.Engine.Common.Xml;

public class SpaceNode : IRenderToText
{
    public int Qty = 1;

    public SpaceNode()
    {
    }

    public Task RenderToTextAsync()
    {
        var ctx = TextRenderContext.Current;
        var sb = ctx.Sb;

        for (var i = 0; i < Qty; i++)
            sb.Append(' ');

        return Task.CompletedTask;
    }
}
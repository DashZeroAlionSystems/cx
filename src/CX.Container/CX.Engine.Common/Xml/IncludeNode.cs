using CX.Engine.Common.CodeProcessing;
using CX.Engine.Common.Rendering;

namespace CX.Engine.Common.Xml;

public class IncludeNode : IRenderToText
{
    public string Prop;

    public async Task RenderToTextAsync()
    {
        if (string.IsNullOrWhiteSpace(Prop))
            return;
        
        var ctx = TextRenderContext.Current;
        var scope = ctx.Scope;
        var val = await Accessor.EvaluateAsync(Prop, scope, "scope");
        if (val != null)
            await val.RenderToTextContextAsync();
    }
}
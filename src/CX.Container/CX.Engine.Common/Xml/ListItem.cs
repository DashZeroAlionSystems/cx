using System.Text;
using CX.Engine.Common.Rendering;
using JetBrains.Annotations;

namespace CX.Engine.Common.Xml;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
[CxmlChildren(true, UnknownAsStrings = true)]
public class ListItem : ICxmlAddChild, ICxmlChildren, IRenderToText, ICxmlHasParentProp
{
    public object Parent { get; set; }
    public List<object> CxmlChildren { get; } = [];

    public async Task AddChildAsync(object o)
    {
        CxmlChildren.Add(o);
    }

    
    public async Task RenderToTextAsync()
    {
        await CxmlChildren.RenderToTextContextAsync();
    }

    IEnumerable<object> ICxmlChildren.CxmlChildren() => CxmlChildren;
}
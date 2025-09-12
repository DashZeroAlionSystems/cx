using CX.Engine.Common.Rendering;

namespace CX.Engine.Common.Xml;

public class CxmlContainerNode : ICxmlAddChild, ICxmlChildren, IRenderToText
{
    [CxmlIgnore]
    public string NodeName;
    
    public readonly List<object> CxmlChildren = [];
    
    public async Task AddChildAsync(object o)
    {
        CxmlChildren.Add(o);
    }

    IEnumerable<object> ICxmlChildren.CxmlChildren() => CxmlChildren;
    
    public Task RenderToTextAsync() => CxmlChildren.RenderToTextContextAsync();
}
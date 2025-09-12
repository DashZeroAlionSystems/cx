using CX.Engine.Common.Formatting;

namespace CX.Engine.Common.Xml;

public static class CxmlCommon
{
    [CxmlFactory]
    public static Foreach Foreach() => new();

    [CxmlFactory][CxmlAction("p")]
    public static Paragraph Paragraph() => new();
    
    [CxmlFactory][CxmlAction("h1")]
    public static Header Header1() => new(1);
    
    [CxmlFactory][CxmlAction("h2")]
    public static Header Header2() => new(2);
    
    [CxmlFactory][CxmlAction("h3")]
    public static Header Header3() => new(3);
    
    [CxmlFactory][CxmlAction("ol")]
    public static OrderedList OrderedList() => new();
    
    [CxmlFactory][CxmlAction("li")]
    public static ListItem ListItem() => new();

    [CxmlFactory][CxmlAction("include")]
    public static IncludeNode Include() => new();

    [CxmlFactory]
    [CxmlAction("prompt-section")]
    public static PromptSectionNode PromptSection() => new();

    [CxmlFactory]
    [CxmlAction("space")]
    public static SpaceNode SpaceNode() => new();

    [CxmlFactory]
    [CxmlAction("container")]
    public static CxmlContainerNode ContainerNode() => new();

    [CxmlFactory]
    [CxmlAction("scope")]
    public static CxmlScopeNode ScopeNode() => new();
}
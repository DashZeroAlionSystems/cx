using System.Dynamic;
using CX.Engine.Common.Formatting;
using SmartFormat;

namespace Cx.Engine.Common.PromptBuilders;

public class PromptContentSection : PromptSection
{
    public string Content;
    public Func<string> ContentFunction;
    public bool ApplySmartFormat = true;
    
    public override string GetContent(ExpandoObject context)
    {
        if (ContentFunction != null)
            Content = ContentFunction();
        
        if (string.IsNullOrWhiteSpace(Content))
            return null;
        
        return ApplySmartFormat ? CxSmart.Format(Content, context) : Content;
    }

    public PromptContentSection()
    {
        Content = "";
        Order = null;
    }

    public PromptContentSection(string content, int? order = null, bool applySmartFormat = true)
    {
        Content = content;
        Order = order;
        ApplySmartFormat = applySmartFormat;
    }

    public PromptContentSection(Func<string> content, int? order = null)
    {
        ContentFunction = content;
        Order = order;
    }
    
}
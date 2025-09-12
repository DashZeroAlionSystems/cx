using System.Dynamic;

namespace Cx.Engine.Common.PromptBuilders;

public abstract class PromptSection
{
    public int? Order;

    /// <summary>
    /// Set by the prompt builder
    /// </summary>
    internal int EffectiveOrder;
    
    public abstract string GetContent(ExpandoObject context);

    public static PromptSection For(string content) => new PromptContentSection(content);
}
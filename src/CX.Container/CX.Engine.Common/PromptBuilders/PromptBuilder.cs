using System.Dynamic;
using System.Text;
using CX.Engine.Common;

namespace Cx.Engine.Common.PromptBuilders;

public class PromptBuilder
{
    private readonly List<PromptSection> Sections = [];
    public readonly dynamic Context = new ExpandoObject();
    public int? TokenLimit;

    public string GetPrompt()
    {
        var sb = new StringBuilder();

        for (var i = 0; i < Sections.Count; i++)
        {
            var section = Sections[i];
            section.EffectiveOrder = section.Order ?? i;
        }

        var first = true;
        var orderedSections = Sections.OrderBy(s => s.EffectiveOrder).ToList();
        var totalTokens = 0;
        foreach (var section in orderedSections)
        {
            var content = section.GetContent(Context);
            
            if (string.IsNullOrWhiteSpace(content))
                continue;

            if (!first)
                sb.AppendLine();
            var seectionContent = content.Trim();
            var sectionTokens = TokenCounter.CountTokens(seectionContent);

            if (TokenLimit.HasValue && totalTokens + sectionTokens > TokenLimit)
                break;
                
            totalTokens += sectionTokens;
            sb.AppendLine(content.Trim());
            first = false;
        }

        return sb.ToString();
    }

    public PromptContentSection Add(string content, int? order = null)
    {
        var section = new PromptContentSection(content, order);
        Sections.Add(section);
        return section;
    }

    public PromptContentSection Add(Func<string> content, int? order = null)
    {
        var section = new PromptContentSection(content, order);
        Sections.Add(section);
        return section;
    }

    public T Add<T>(T section) where T: PromptSection
    {
        Sections.Add(section);
        return section;
    }
}
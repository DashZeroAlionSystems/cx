using System.Dynamic;
using System.Text;
using CX.Engine.Common;

namespace Cx.Engine.Common.PromptBuilders;

public class TreePromptBuilder
{
    private readonly List<PromptSection> Sections = [];
    public readonly dynamic Context = new ExpandoObject();
    public int? TokenLimit;
    public int IndentSize = 4;
    
    public string GetPrompt()
    {
        var sb = new StringBuilder();
        var totalTokens = 0;

        for (var i = 0; i < Sections.Count; i++)
        {
            var section = Sections[i];
            section.EffectiveOrder = section.Order ?? throw new ArgumentNullException($"{nameof(section.Order)} cannot be null.");
        }
        
        foreach (var section in Sections)
        {
            var indent = new string(' ', section.EffectiveOrder * IndentSize);
            var content = section.GetContent(Context);
            
            if (string.IsNullOrWhiteSpace(content))
                continue;
            
            var sectionContent = content.Trim();
            var sectionTokens = TokenCounter.CountTokens(sectionContent);

            if (TokenLimit.HasValue && totalTokens + sectionTokens > TokenLimit)
                break;

            totalTokens += sectionTokens;
            sb.AppendLine($"{indent}{sectionContent}");
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
using CX.Engine.Common;

namespace CX.Engine.QAndA;

public class IgnoreCriteria : BaseCriteria
{
    public readonly string Criteria;

    public IgnoreCriteria(string criteria, HashSet<string> questionTags = null)
    {
        Criteria = MiscHelpers.StripHashtags(criteria) ?? throw new ArgumentNullException(nameof(criteria));
        
        if (questionTags != null)
            Tags.AddRange(questionTags);
        
        Tags.AddRange(MiscHelpers.ExtractHashtags(criteria));
    }

    public override string GetCellContent(HashSet<string> entryTags)
    {
        return "- [Ignore] " + (Criteria + " " + string.Join(' ', Tags.Except(entryTags)))
            .Trim();
    }
}
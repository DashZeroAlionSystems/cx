using CX.Engine.Common;

namespace CX.Engine.QAndA;

public class AttachCriteria : BaseCriteria
{
    public readonly string Name;
    public readonly string Sha256;
    
    public AttachCriteria(string name, string sha256, string question, HashSet<string> questionTags)
    {
        var strippedName = MiscHelpers.StripHashtags(name);
        
        if (string.IsNullOrWhiteSpace(strippedName))
            throw new ArgumentException($"Invalid attach criteria name for question {question}: {name}", nameof(name));
        
        Name = strippedName ?? throw new ArgumentNullException(nameof(name));
        Sha256 = sha256;

        if (questionTags != null)
            Tags.AddRange(questionTags);
        
        Tags.AddRange(MiscHelpers.ExtractHashtags(name));
    }

    public override string GetCellContent(HashSet<string> entryTags)
    {
        return "- [Attach] " + (Name + " " + string.Join(' ', Tags.Except(entryTags)) + "\n" + Sha256)
            .Trim();
    }
}
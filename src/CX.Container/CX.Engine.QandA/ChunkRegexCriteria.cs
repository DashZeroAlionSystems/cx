using System.Text.RegularExpressions;
using CX.Engine.Common;

namespace CX.Engine.QAndA;

public class ChunkRegexCriteria : BaseCriteria
{
    public readonly string Name;
    public readonly string Regex;
    public readonly Regex InstantiatedRegex;
    
    public ChunkRegexCriteria(string name, string regex, string question, HashSet<string> questionTags)
    {
        var strippedName = MiscHelpers.StripHashtags(name);
        
        if (string.IsNullOrWhiteSpace(strippedName))
            throw new ArgumentException($"Invalid chunk criteria name for question {question}: {name}", nameof(name));
        
        Name = strippedName ?? throw new ArgumentNullException(nameof(name));
        Regex = regex ?? throw new ArgumentNullException(nameof(regex));
        //Validate regex
        try
        {
            InstantiatedRegex = new(Regex, RegexOptions.Compiled | RegexOptions.IgnoreCase);
        }
        catch (Exception ex)
        {
            throw new ArgumentException($"Invalid regex for question {question}'s chunk criteria {Name}: {Regex}", nameof(regex), ex);
        }

        if (questionTags != null)
            Tags.AddRange(questionTags);
        
        Tags.AddRange(MiscHelpers.ExtractHashtags(name));
    }

    public override string GetCellContent(HashSet<string> entryTags)
    {
        return "- [Regex] " + (Name + " " + string.Join(' ', Tags.Except(entryTags)) + "\n" + Regex)
            .Trim();
    }
}
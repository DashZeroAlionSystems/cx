namespace CX.Engine.QAndA;

public abstract class BaseCriteria
{
    public readonly HashSet<string> Tags = new();

    public abstract string GetCellContent(HashSet<string> entryTags);

    public static BaseCriteria FromString(string s, string question, HashSet<string> questionTags)
    {
        s = s.Trim();

        if (s.StartsWith("[Regex]", StringComparison.InvariantCultureIgnoreCase))
        {
            s = s.Substring("[Regex]".Length).Trim();
            var lines = s.Split('\n');
            if (lines.Length != 2)
                throw new InvalidOperationException($"[Regex] expression without 2 lines: {s} for question {question}");

            var name = lines[0].Trim();
            if (name.EndsWith(':'))
                name = name.Substring(0, name.Length - 1);

            var regex = lines[1].Trim();
            return new ChunkRegexCriteria(name, regex, question, questionTags);
        }

        if (s.StartsWith("[Ignore]", StringComparison.InvariantCultureIgnoreCase))
        {
            s = s.Substring("[Ignore]".Length).Trim();
            return new IgnoreCriteria(s, questionTags);
        }

        if (s.StartsWith("[Attach]", StringComparison.InvariantCultureIgnoreCase))
        {
            s = s.Substring("[Attach]".Length).Trim();
            var lines = s.Split('\n');

            var name = lines[0].Trim();
            if (name.EndsWith(':'))
                name = name.Substring(0, name.Length - 1);
            
            var fileId = lines.Length  >= 2 ? lines[1].Trim() : null;
            return new AttachCriteria(name, fileId, question, questionTags);
        }

        return new NLCriteria(s, questionTags);
    }
}
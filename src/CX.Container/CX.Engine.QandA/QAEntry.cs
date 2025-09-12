using CX.Engine.Common;
using CX.Engine.Assistants;

namespace CX.Engine.QAndA;

public class QAEntry
{
    public string Question;
    public readonly List<BaseCriteria> Criteria = new();
    public readonly HashSet<string> Tags = new();
    public readonly List<(bool pass, string detail)> OutcomeDetail = new();
    public AssistantAnswer Answer;
    public double? Outcome;
    public string Notes;
    public List<RankedChunk> Chunks;
    public HashSet<AttachmentInfo> AttachmentsInEval;
    public readonly HashSet<AttachmentInfo> AttachmentsMatched = new();
    public readonly Dictionary<string, bool> CriteriaGroups = new(StringComparer.InvariantCultureIgnoreCase);
    public string ChannelName { get; set; }
    
    public QAEntry()
    {
    }

    public QAEntry(string question, params string[] evals)
    {
        Question = question;
        Criteria.AddRange(from e in evals select BaseCriteria.FromString(e, question, null));
    }
}
using System.Text;
using CX.Engine.Assistants.AssessmentBuilder.Xml;
using CX.Engine.Archives;
using CX.Engine.Archives.PgVector;

namespace CX.Engine.Assistants.AssessmentBuilder;

public class BaseNodeUtils
{
    public readonly AssessmentAssistant.Snapshot Snapshot;
    public readonly AssessmentPaper Paper;
    
    public BaseNodeUtils(AssessmentAssistant.Snapshot snapshot, AssessmentPaper paper)
    {
        Snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
        Paper = paper ?? throw new ArgumentNullException(nameof(paper));
    }

    public async Task<string> ContextForPromptAsync(string prompt)
    {
        var opts = Snapshot.Options;

        var req = new ChunkArchiveRetrievalRequest()
        {
            CutoffTokens = opts.QuestionContextCutoffTokens,
            MinSimilarity = opts.QuestionContextMinSimilarity,
            QueryString = prompt
        };
        var where = new PgVectorAppendWhere()
        {
            Where = "AND metadata->'info'->>'Grade' = @grade AND metadata->'info'->>'Subject' = @subject"
        };
        where.Parameters = new () {
            ["@grade"] = Paper.Grade.ToString(),
            ["@subject"] = Paper.Subject 
        };
        req.Components.Add(where);        
        var matches = await Snapshot.Archive.RetrieveAsync(req);

        var sb = new StringBuilder();
        sb.AppendLine("Here are a few contextual matches from our vector database that might help you with this request:");
        sb.AppendLine();

        foreach (var match in matches)
        {
            sb.AppendLine(match.Chunk.GetContextString());
            sb.AppendLine();
        }

        return sb.ToString();
    }
}
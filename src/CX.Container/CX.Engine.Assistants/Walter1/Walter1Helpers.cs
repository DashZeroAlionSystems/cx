using CX.Engine.Archives;

namespace CX.Engine.Assistants.Walter1;

public static class Walter1Helpers
{
    public class TopKScore
    {
        public readonly List<double> Scores = new();
        public double FinalScore;
        public int Chunks;
        public int Tokens;
    }

    /// <summary>
    /// Only keep the chunks from the top k documents by avg(top3(chunk.score)) based on <see cref="ArchiveMatch.GroupId"/>.
    /// </summary>
    public static List<ArchiveMatch> TopKDocuments(AssistantAnswer res, List<ArchiveMatch> matches, int k)
    {
        var groups = new Dictionary<string, TopKScore>();
        foreach (var match in matches)
        {
            var groupId = match.GroupId;
                    
            if (!groups.TryGetValue(groupId, out var group))
                group = new();

            group.Scores.Add(match.Score);
            group.Chunks++;
            group.Tokens += match.Chunk.EstTokens;

            groups[groupId] = group; 
        }

        foreach (var score in groups.Values)
            score.FinalScore = score.Scores.OrderByDescending(s => s).Take(3).Average();
        
        var topGroups = groups.OrderByDescending(m => m.Value.FinalScore).Take(k).ToList();
        res.DocumentFilter = topGroups;
        return matches.Where(m => topGroups.Any(g => g.Key == m.GroupId)).ToList();
    }
}
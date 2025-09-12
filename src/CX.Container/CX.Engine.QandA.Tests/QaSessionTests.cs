using CX.Engine.QAndA;
using Xunit;

namespace CXLibTests;

public class QaSessionTests
{
    [Fact]
    public async Task SaveToWordRunsTest()
    {
        var qaDoc = new QASession();

        qaDoc.LastScore = new()
        {
            Overall = 1,
            OverallMax = 4,
            TotalCriteria = 4
        };

        qaDoc.Entries.Add(new()
        {
            Question = "What color are apples?",
            Answer = new("They can be red.\n\nIn computer games, they can be any color.\n\nBut in real life, they are usually red."),
            Criteria = { new NLCriteria("- Can be green or red.") },
            Outcome = 0.5,
            OutcomeDetail =
            {
                new(true, "Mentions red\nThe answer mentions red.\n\n**Final Pass**"),
                new(false, "Mentions green\nThe answer does not mention red.\n\n**Final Fail**")
            }
        });

        qaDoc.Entries.Add(new()
        {
            Question = "What color are pears?",
            Answer = new("They can be blue."),
            Criteria = { new NLCriteria("- Can be green or yellow.") },
            Outcome = 0,
            OutcomeDetail = { new(false, "Mentions yellow\nLine 2\nLine 3"), new(false, "Mentions green\nLine 2\nLine 3") }
        });

        var filePath = Path.GetTempFileName();
        File.Delete(filePath);
        filePath = Path.ChangeExtension(filePath, ".docx");
        try
        {
            await qaDoc.SaveToWordAsync(filePath);
        }
        finally
        {
            if (File.Exists(filePath))
                File.Delete(filePath);
        }
    }
}
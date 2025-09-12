using System.Diagnostics;
using System.Text;
using System.Text.Json;
using CX.Engine.Assistants;
using CX.Engine.Common;
using CX.Engine.Common.PostgreSQL;

namespace CX.Engine.QAndA.Structured;

public class StructuredTestRunner<TRoot> 
{
    private readonly Action<string> _printLine;
    
    public class TestCase : StructuredTestCase<StructuredTestRunner<TRoot>, TRoot>;

    public readonly PostgreSQLClient Client;
    public readonly IAssistant Assistant;

    public int TotalFinished;

    public readonly List<TestCase> Tests = new();

    public async Task<TRoot> AskAsync(string question)
    {
        var answer = (await Assistant.AskAsync(question, new())).Answer;
        var result = JsonSerializer.Deserialize<TRoot>(answer!);
        return result;
    }

    public async Task RunTestAsync(TestCase test)
    {
        TRoot res;
        var sw = Stopwatch.StartNew();

        test.Exception = null;
        test.TotalMilliseconds = 0;

        try
        {
            res = await AskAsync(test.Question);
            test.Run = this;
            double score = 1;
            test.Response = res;
            test.Score = null;
            if (test.GetScoreAsync != null)
                score = await test.GetScoreAsync(test);
            if (test.GetScore != null)
                score = test.GetScore(test);

            test.Score = score;
        }
        catch (Exception ex)
        {
            test.Exception = ex;
            res = default;
        }

        Interlocked.Increment(ref TotalFinished);

        //if (!passed && Debugger.IsAttached)
        //    Debugger.Break();

        test.TotalMilliseconds = sw.ElapsedMilliseconds;
        var sb = new StringBuilder();
        sb.Append($"{test.Name,-50}:");
        
        if (test.Score != null)
            sb.Append($" {test.Score,5:P0}");
        else
            sb.Append("      ");
        
        sb.Append($" ({test.TotalMilliseconds:#,##0}ms)");
        if (test.Exception != null)
            sb.Append($" with {test.Exception.Message}");
        _printLine(sb.ToString());
    }

    public async Task<double> RunTestsAsync()
    {
        _printLine("Running...");

        var sw = Stopwatch.StartNew();
        await (from test in Tests select RunTestAsync(test));

        var totalScore = 0.0;
        var totalScored = 0;
        foreach (var test in Tests)
        {
            if (test.Score.HasValue)
            {
                totalScore += test.Score.Value;
                totalScored++;
            }
        }

        _printLine("------------------------");

        var sb = new StringBuilder();
        sb.AppendLine($"Completed in {sw.ElapsedMilliseconds}ms ({Tests.Count} tests)");
        if (totalScored > 0)
            sb.AppendLine($"Average score: {totalScore / totalScored,5:P0} ({totalScore:#,##0.##} total score over {totalScored:#,##0} scored tests)");
        _printLine(sb.ToString());

        return totalScore / totalScored;
    }

    public StructuredTestRunner(PostgreSQLClient client, IAssistant assistant, Action<string> printLine)
    {
        Client = client ?? throw new ArgumentNullException(nameof(client));
        Assistant = assistant ?? throw new ArgumentNullException(nameof(assistant));
        _printLine = printLine ?? throw new ArgumentNullException(nameof(printLine));
    }
}
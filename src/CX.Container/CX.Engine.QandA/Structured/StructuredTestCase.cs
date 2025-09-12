namespace CX.Engine.QAndA.Structured;

public class StructuredTestCase<TRun, TRoot>
{
    public TRun Run;
    public TRoot Response;
    public string Name;
    public string Question;
    public Func<StructuredTestCase<TRun, TRoot>, Task<double>> GetScoreAsync;
    public Func<StructuredTestCase<TRun, TRoot>, double> GetScore;
    public double TotalMilliseconds;
    public double? Score;
    public Exception Exception;
    
    public void Assert(bool condition, string throwMessage)
    {
        if (!condition)
            throw new Exception(throwMessage);
    }
}
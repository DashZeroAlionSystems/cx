using System.Text;

namespace CX.Engine.QAndA;

public class QAScore
{
    public double Overall;
    public double OverallMax;
    public double OverallPercentage => OverallMax == 0 ? 1 : Overall / OverallMax;
    public int TotalCriteria;
    public int TotalRegex;
    public int TotalAttach;
    public int TotalIgnored;
    public readonly Dictionary<string, double> ByTagScore = new();
    public readonly Dictionary<string, double> ByTagMax = new();

    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Overall: {OverallPercentage:#,##0%} ({Overall:#,##0.00} / {OverallMax:#,##0.00})");
        sb.AppendLine(
            $"Total Criteria: {TotalCriteria:#,##0} (of which {TotalIgnored:#,##0} ignored, {TotalRegex:#,##0} regex, {TotalAttach:#,##0} attach and {TotalCriteria - TotalRegex - TotalAttach:#,##0} natural language)");
        sb.AppendLine();

        foreach (var key in ByTagScore.Keys.ToList())
            sb.AppendLine($"{key}: {GetTagPercent(key):#,##0%} ({ByTagScore[key]:#,##0.00} / {ByTagMax[key]:#,##0.00})");

        return sb.ToString();
    }

    /// <summary>
    /// Thread-safe
    /// </summary>
    /// <param name="tag"></param>
    /// <param name="percent"></param>
    public void IncTag(string tag, double percent)
    {
        if (percent > 1 || percent < 0)
            throw new ArgumentOutOfRangeException(nameof(percent), percent, "Percent must be between 0 and 1");
        
        lock (this)
        {
            if (ByTagScore.TryAdd(tag, percent))
            {
                ByTagMax[tag] = 1;
                return;
            }

            ByTagScore[tag] += percent;
            ByTagMax[tag]++;
        }
    }
    
    public double GetTagPercent(string tag)
    {
        var max = ByTagMax.GetValueOrDefault(tag);
        
        if (max == 0)
            return 1;

        var score = ByTagScore.GetValueOrDefault(tag);
        
        return score / max;
    }
    
    public bool PassesAll(string tag)
    {
        return (int)ByTagScore.GetValueOrDefault(tag) == (int)ByTagMax.GetValueOrDefault(tag);
    }
    
    public bool PassesAllCrits() => PassesAll("crit");
}
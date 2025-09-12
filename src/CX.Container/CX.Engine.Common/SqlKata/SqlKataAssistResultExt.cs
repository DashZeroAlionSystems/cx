using System.Text;
using System.Text.RegularExpressions;
using JetBrains.Annotations;

namespace CX.Engine.Common.SqlKata;

public static class SqlKataAssistResultExt
{
    private static readonly List<string> SupportedAggregates = ["SUM", "AVG", "MIN", "MAX"];
    private static string CalculateIndent(int order, int indentSize) => new string(' ', indentSize * order);
    public static string ToSelectionTree(this SqlKataAssistSelection results, int order = 0, int indentSize = 4)
    {
        var sb = new StringBuilder();
        var indent = CalculateIndent(order, indentSize);
        order += 1;
        var itemIndent = CalculateIndent(order, indentSize);
        foreach (var result in results)
        {
            sb.AppendLine($"{indent} {result.Key}:");
            sb.AppendLine($"{itemIndent}{string.Join(", ", result.Value)}");
        }
        
        return sb.ToString();
    }

    private static bool CheckForSupportedAggragate(this string select)
    {
        foreach(var aggregate in SupportedAggregates)
            if(select.Contains(aggregate))
                return true;
        
        return false;
    }
    
    public static void SelectHandleRename(this SqlKataFormats formats, [NotNull] string newName)
    {
        foreach (var format in formats)
            if (newName.Contains(format.Key) && newName.Contains(" AS ", StringComparison.CurrentCultureIgnoreCase) && newName.CheckForSupportedAggragate())
                formats.UpdateKey(format.Key, Regex.Split(newName, " AS ", RegexOptions.IgnoreCase).Last().TrimStart());
    }

    public static string SelectToAlias(this string select)
    {
        var aggr = Regex.Split(select, " AS ", RegexOptions.IgnoreCase).Last().TrimEnd();
        return aggr;
    }

    public static string SelectToAggregate(this string select)
    {
        var aggr = Regex.Split(select, " AS ", RegexOptions.IgnoreCase).First().TrimEnd();
        return aggr;
    }
}
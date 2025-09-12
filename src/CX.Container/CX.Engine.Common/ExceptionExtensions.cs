using System.Text;

namespace CX.Engine.Common;

public static class ExceptionExtensions
{
    public static string GetExceptionDetails(this Exception ex)
    {
        var sb = new StringBuilder();
        AppendExceptionDetails(sb, ex, "");
        return sb.ToString();
    }

    private static void AppendExceptionDetails(StringBuilder sb, Exception ex, string indent)
    {
        if (ex == null) return;

        sb.AppendLine($"{indent}Exception Type: {ex.GetType().FullName}");
        sb.AppendLine($"{indent}Message: {ex.Message}");
        sb.AppendLine($"{indent}Stack Trace: {ex.StackTrace}");

        if (ex is AggregateException aggEx)
        {
            foreach (var innerEx in aggEx.InnerExceptions)
            {
                sb.AppendLine($"{indent}Inner Exception:");
                AppendExceptionDetails(sb, innerEx, indent + "  ");
            }
        }
        else if (ex.InnerException != null)
        {
            sb.AppendLine($"{indent}Inner Exception:");
            AppendExceptionDetails(sb, ex.InnerException, indent + "  ");
        }
    }
}
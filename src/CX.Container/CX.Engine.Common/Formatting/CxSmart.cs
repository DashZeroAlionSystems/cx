using SmartFormat;
using SmartFormat.Extensions;

namespace CX.Engine.Common.Formatting;

public static class CxSmart
{
    public static string SmartFormatEscape(this string input, bool escape = true)
    {
        return input == null ? null : escape ? input.Replace("{", "\\{").Replace("}", "\\}") : input;
    }

    public static string Format(string format, object context = null)
    {
        if (format == null)
            return null;

        //var formatter = Smart.CreateDefaultSmartFormat();
        //formatter.AddExtensions(new DictionarySource() { IsIReadOnlyDictionarySupported = true });

        return Smart.Format(format, context);
    }

    public static async Task<string> LazyFormatAsync(string s, CxSmartScope scope)
    {
        if (s == null)
            return null;
        
        if (scope == null)
            throw new ArgumentNullException(nameof(scope));
        
        scope.SetToStubMode();
        Format(s, scope.GetFullContext());
        await scope.ResolveAsync();
        return Format(s, scope.GetFullContext());
    }

    public static async Task<string> LazyFormatAsync(string s, StubbedLazyDictionary context = null)
    {
        if (s == null)
            return null;

        context?.SetToStubMode();
        Format(s, context);
        if (context != null)
            await context.ResolveAsync();
        return Format(s, context);
    }
}
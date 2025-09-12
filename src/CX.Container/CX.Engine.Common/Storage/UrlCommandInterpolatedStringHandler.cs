using System.Runtime.CompilerServices;
using System.Text;

namespace CX.Engine.Common.Storage;

[InterpolatedStringHandler]
public struct UrlCommandInterpolatedStringHandler
{
    private readonly StringBuilder _builder;
    
    public UrlCommandInterpolatedStringHandler(int literalLength, int formattedCount)
    {
        _builder = new(literalLength + formattedCount * 20);
    }
    
    public void AppendLiteral(string s)
    {
        _builder.Append(s);
    }

    public void AppendFormatted<T>(T t, string format) where T : IFormattable
    {
        _builder.Append(t?.ToString(format, null));
    }

    public void AppendFormatted<T>(T t)
    {
        _builder.Append(Flurl.Url.Parse(t?.ToString() ?? ""));
    }
    
    public string GetString()
    {
        return _builder.ToString();
    }
}
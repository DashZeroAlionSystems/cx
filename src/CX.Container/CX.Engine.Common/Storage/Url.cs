using System.Text;

namespace CX.Engine.Common.Storage;

public class Url
{
    public static string Combine(string baseUrl, params UrlCommandInterpolatedStringHandler[] args)
    {
        StringBuilder builder = new(baseUrl);
        foreach (var arg in args)
            builder.Append(arg.GetString());
        return builder.ToString();
    }
}
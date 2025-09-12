using Tiktoken;

namespace CX.Engine.Common;

/// <summary>
/// A wrapper for TokenCounting that abstracts away the actual encoding and library used.
/// TikToken for gpt-3.5-turbo is currently implemented.
/// </summary>
public static class TokenCounter
{
    private static readonly Encoding Encoding = Encoding.ForModel("gpt-3.5-turbo");
    
    public static int CountTokens(string content)
    {
        if (content == null)
            return 0;
        else
            return Encoding.CountTokens(content);
    }
}
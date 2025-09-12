namespace CX.Engine.Common.PostgreSQL;

/// <summary>
/// Used to inject raw SQL into a command string built by <see cref="NpgsqlCommandInterpolatedStringHandler"/>.
/// </summary>
public struct InjectRaw
{
    public string Content;

    public InjectRaw(string content)
    {
        Content = content;
    }
}
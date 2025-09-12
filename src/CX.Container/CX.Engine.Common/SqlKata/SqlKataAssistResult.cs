namespace CX.Engine.Common.SqlKata;

public class SqlKataAssistResult
{
    public string Sql { get; set; }
    public IEnumerable<dynamic> Results { get; set; }
    public SqlKataAssistSelection Selections { get; set; } = new();
    public SqlKataFormats Formats { get; set; } = new();
}
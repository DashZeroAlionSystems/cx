namespace CX.Engine.Common.SqlKata;

public class SqlKataRequest
{
    public List<SqlKataFunctionType> Functions { get; set; } = [];
    public string Name { get; set; }
    public string Type { get; set; }
    public string Description { get; set; }
    public bool IsDate { get; set; }
    public List<string> Choices { get; set; } = [];
    public object DefaultValue { get; set; }
    public string Format { get; set; }
    public bool AllowMultiple { get; set; }
}
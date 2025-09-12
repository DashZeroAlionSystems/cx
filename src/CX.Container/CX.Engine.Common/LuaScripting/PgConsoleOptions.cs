namespace CX.Engine.Common;

public class PgConsoleOptions : IValidatable
{
    public string PostgreSQLClientName { get; set; }
    public string LuaCoreName { get; set; }
    public TimeSpan CheckInterval { get; set; } = TimeSpan.FromSeconds(1);

    public void Validate()
    {
    }
}
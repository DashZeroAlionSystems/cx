namespace CX.Engine.Common;

public class LuaCoreOptions : IValidatable
{
    public string[] Libraries { get; set; }

    public void Validate()
    {
    }
}
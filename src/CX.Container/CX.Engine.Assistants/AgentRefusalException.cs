namespace CX.Engine.Assistants;

public class AgentRefusalException : Exception
{
    public AgentRefusalException(string message) : base(message)
    {
    }
}
namespace CX.Engine.ChatAgents.OpenAI;

public class OpenAIRefusalException : Exception
{
    public OpenAIRefusalException(string message): base(message)
    {
    }
}
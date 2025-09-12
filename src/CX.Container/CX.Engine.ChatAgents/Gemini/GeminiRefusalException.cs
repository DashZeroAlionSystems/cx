namespace CX.Engine.ChatAgents.Gemini;

public class GeminiRefusalException : Exception
{
    public GeminiRefusalException(string message) : base(message)
    {
        
    }
}
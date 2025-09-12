using CX.Engine.Common;

namespace CX.Engine.ChatAgents.Gemini;

public class GeminiChatAgentOptions: IValidatable
{
    public string APIKey { get; set; } = null!;
    public string Model { get; set; } = null!;
    public int MaxConcurrentCalls { get; set; }
    public bool StripMarkdownLinks { get; set; }
    public bool OnlyUserRole { get; set; }
    public double DefaultTemperature { get; set; } = 0.3;
    public void Validate()
    {
    }
}
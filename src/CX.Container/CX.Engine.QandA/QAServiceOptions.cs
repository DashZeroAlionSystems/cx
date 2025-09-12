using CX.Engine.Common;

namespace CX.Engine.QAndA;

public class QAServiceOptions : IValidatable
{
    public string AssistantName { get; set; }
    public string AgentName { get; set; }

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(AssistantName))
            throw new InvalidOperationException($"{nameof(AssistantName)} is required.");
    }
}
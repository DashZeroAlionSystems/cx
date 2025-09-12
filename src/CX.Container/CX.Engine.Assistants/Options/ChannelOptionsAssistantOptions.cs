using System.ComponentModel.DataAnnotations;
using CX.Engine.Common;

namespace CX.Engine.Assistants.Options;

public class ChannelOptionsAssistantOptions : IValidatable
{
    public string PostgreSQLClientName { get; set; } = "pg_default";
    
    //public string AgentName { get; set; } = "OpenAI.o1";
    public bool UseExecutionPlan { get; set; } = true;

    public string AgentName { get; set; } = "OpenAI.GPT-4o-mini";

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(AgentName))
            throw new ValidationException($"{nameof(AgentName)} must be set.");
        
        if (string.IsNullOrWhiteSpace(PostgreSQLClientName))
            throw new ValidationException($"{nameof(PostgreSQLClientName)} must be set.");
    }
}
using CX.Engine.Common;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;

namespace CX.Engine.Assistants.QueryAssistants;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class SqlServerQueryAssistantOptions : IValidatable
{
    public string SchemaDefinition { get; set; } = "";
    public string ChatAgentName { get; set; }
    public string SystemPrompt { get; set; }

    public bool CacheQuestions { get; set; }

    public string CachePostgreSQLClientName { get; set; }
    public string CacheTableName { get; set; }

    public bool CanExecuteQueries { get; set; }
    public string SQLServerClientName { get; set; }
    
    public bool UseExecutionPlan { get; set; } = true;
    public bool DebugMode { get; set; } = false;
    
    public string ReasoningEffort { get; set; } = "high";
    public bool SelectOnly { get; set; } = true;

    public int LangfuseMaxStringLen { get; set; } = 50_000;

    public bool KeepLastMessageOnly { get; set; } = false;
    public LogLevel MessageLogLevel { get; set; } = LogLevel.Debug;
    
    public bool DiscordMode { get; set; }
    public int? MaxRows { get; set; } = 100;

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(SchemaDefinition))
            throw new ArgumentException($"{nameof(SchemaDefinition)} is required.");

        if (string.IsNullOrWhiteSpace(ChatAgentName))
            throw new ArgumentException($"{nameof(ChatAgentName)} is required.");

        if (CacheQuestions)
        {
            if (string.IsNullOrWhiteSpace(CachePostgreSQLClientName))
                throw new ArgumentException($"{nameof(CachePostgreSQLClientName)} is required when CacheQuestions is enabled.");
            
            if (string.IsNullOrWhiteSpace(CacheTableName))
                throw new ArgumentException($"{nameof(CacheTableName)} is required when CacheQuestions is enabled.");
        }

        if (CanExecuteQueries)
        {
            if (string.IsNullOrWhiteSpace(SQLServerClientName))
                throw new ArgumentException($"{nameof(SQLServerClientName)} is required when CanExecuteQueries is enabled.");
        }
        
        if (LangfuseMaxStringLen < 1)
            throw new ArgumentException($"{nameof(LangfuseMaxStringLen)} must be greater than 0.");
    }
}
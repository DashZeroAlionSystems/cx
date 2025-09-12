using System.Text.Json.Serialization;
using CX.Engine.Common;
using CX.Engine.Common.Json;
using JetBrains.Annotations;

namespace CX.Engine.Assistants.QueryAssistants;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class SqlServerReportAssistantOptions : IValidatable
{
    public string ChatAgentName { get; set; }
    public string SqlServerClientName { get; set; }
    public string SystemPrompt { get; set; }
    public string LlmJsonHeaderPrompt { get; set; }
    public int MaxLlmMarkdownLengthFromRunQuery { get; set; } = 5000;
    public List<SqlServerReportParameter> Parameters { get; set; }
    public int? MaxRows { get; set; } = 20;
    public bool CustomNoQueryResponseMessageEnabled { get; set; }
    public string[] Init { get; set; }
    public string CustomNoQueryResponseMessage { get; set; }
    public bool RunQuery { get; set; } = true;
    public bool DiscordMode { get; set; }
    public string RelationName { get; set; }
    public int MaxConcurrentQuery { get; set; } = 3;
    public string Sql { get; set; }
    public bool CompileSchema { get; set; }
    public bool ShowRawSql { get; set; }
    public int MaxCharactersPerCall { get; set; }
    public bool ShowSelections { get; set; }
    public int CutoffHistoryTokens { get; set; }
    public List<string> CompileKeys { get; set; }
    public bool AllowLimits { get; set; }
    public bool AllowDuplicateExceptions { get; set; } = true;
    public string AllowDuplicateExceptionsMessage { get; set; }
    public bool AllowOneShot { get; set; }

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(ChatAgentName))
            throw new InvalidOperationException($"{nameof(ChatAgentName)} is required.");
        
        if (string.IsNullOrWhiteSpace(SqlServerClientName))
            throw new InvalidOperationException($"{nameof(SqlServerClientName)} is required.");
        
        if (CustomNoQueryResponseMessageEnabled && string.IsNullOrWhiteSpace(CustomNoQueryResponseMessage))
            throw new InvalidOperationException($"{nameof(CustomNoQueryResponseMessage)} is required when {nameof(CustomNoQueryResponseMessageEnabled)} is true.");
        
        if (string.IsNullOrWhiteSpace(RelationName))
            throw new InvalidOperationException($"{nameof(RelationName)} is required.");
        
        if (CutoffHistoryTokens == 0)
            throw new InvalidOperationException($"{nameof(CutoffHistoryTokens)} is required.");

        if (!AllowDuplicateExceptions && string.IsNullOrWhiteSpace(AllowDuplicateExceptionsMessage))
            throw new InvalidOperationException($"{nameof(AllowDuplicateExceptionsMessage)} is required when {nameof(AllowDuplicateExceptions)} is false.");

        Parameters ??= [];

        foreach (var par in Parameters)
            par.Validate();
    }
}
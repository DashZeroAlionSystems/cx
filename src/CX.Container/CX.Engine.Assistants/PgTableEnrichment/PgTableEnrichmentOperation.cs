using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using CX.Engine.Common;
using CX.Engine.Common.Json;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;

namespace CX.Engine.Assistants.PgTableEnrichment;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class PgTableEnrichmentOperation : IValidatable
{
    public string Id { get; set; }
    public string Description { get; set; }
    public string StartMessage { get; set; }
    public string FinishMessage { get; set; }
    public string UpdateScript { get; set; }
    public string UpdateSql { get; set; }

    public string Prompt { get; set; }
    
    public string PostgreSQLClientName { get; set; }
    public string TableName { get; set; }

    public string ChannelName { get; set; }
    
    public string RetrieveSql { get; set; }
    
    public int RowLimit { get; set; }

    [UseJsonDocumentSetup]
    [JsonInclude]
    public JsonNode ResponseSchema;

    public string Init { get; set; }

    public int PromptPreviewLimit { get; set; }
    public int MaxParallelRows { get; set; } = 1;

    public string RowIdentifier { get; set; }

    public LogLevel QuestionLogLevel { get; set; } = LogLevel.None;

    public LogLevel AnswerLogLevel { get; set; } = LogLevel.None;
    
    public LogLevel OpInfoLogLevel { get; set; } = LogLevel.None;
    
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Id))
            throw new InvalidOperationException($"{nameof(Id)} is required.");
        
        if (string.IsNullOrWhiteSpace(Description))
            throw new InvalidOperationException($"{nameof(Description)} is required.");
        
        if (string.IsNullOrWhiteSpace(UpdateSql))
            throw new InvalidOperationException($"{nameof(UpdateSql)} is required.");

        if (string.IsNullOrWhiteSpace(Prompt))
            throw new InvalidOperationException($"{nameof(Prompt)} is required.");
        
        if (string.IsNullOrWhiteSpace(TableName))
            throw new InvalidOperationException($"{nameof(TableName)} is required.");
        
        if (string.IsNullOrWhiteSpace(ChannelName))
            throw new InvalidOperationException($"{nameof(ChannelName)} is required.");
        
        if (string.IsNullOrWhiteSpace(PostgreSQLClientName))
            throw new InvalidOperationException($"{nameof(PostgreSQLClientName)} is required.");
        
        if (string.IsNullOrWhiteSpace(RowIdentifier))
            throw new InvalidOperationException($"{nameof(RowIdentifier)} is required.");
        
        if (string.IsNullOrWhiteSpace(StartMessage))
            throw new InvalidOperationException($"{nameof(StartMessage)} is required.");
        
        if (string.IsNullOrWhiteSpace(FinishMessage))
            throw new InvalidOperationException($"{nameof(FinishMessage)} is required.");

        if (RowLimit < -1)
            throw new InvalidOperationException($"{nameof(RowLimit)} must be -1 or greater.");
        
        if (MaxParallelRows < 1)
            throw new InvalidOperationException($"{nameof(MaxParallelRows)} must be 1 or greater.");
    }
}
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using CX.Engine.Common;
using JetBrains.Annotations;

namespace CX.Engine.Assistants.FlatQuery;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class FlatQueryAssistantOptions : IValidatable
{
    public bool EnableSortSchemaSwapping { get; set; }
    public bool EnableFilterSchemaSwapping { get; set; }
    public bool EnableIntroSchemaSwapping { get; set; }
    public bool EnableSemanticSchemaSwapping { get; set; }
    public TimeSpan SchemaSwapCompletionTimeSpan { get; set; }
    public string PostgreSQLClientName { get; set; }
    public string AnswerLogPostgresSQLClientName { get; set; }
    public string OpenAIAgentName { get; set; }
    public string GeminiSchemaPath { get; set; }
    public string AgentName { get; set; }
    public string SemanticFilterAgentName { get; set; }
    public string IntroAgentName { get; set; }
    public string DbFilterAgentName { get; set; }
    public string SemanticFilterOutToAnswerAgentName { get; set; }
    public string SemanticFilterPredictedOutput { get; set; }
    public int? SemanticFilterMaxCompletionTokens { get; set; }
    public int? DbFilterMaxCompletionTokens { get; set; }
    public string DbFilterPredictedOutput { get; set; }
    public string[] DbFilterPromptLines { get; set; }
    public string DbFilterPrompt { get; set; }
    public bool EnableQueryCache { get; set; }
    public bool UseChatCache { get; set; }
    public bool EnableSchemaSwapping { get; set; }
    public bool UseDbFilter { get; set; } = true;
    public double? ChatCacheCheckSeconds { get; set; }
    public double? ExpireOnCreationMinutes { get; set; }
    public string[] SemanticFilterPromptLines { get; set; }
    public int AnswerLogExpiryHours { get; set; }
    public string IntroPrompt { get; set; }
    public string[] IntroPromptLines { get; set; }
    public bool UseSoftFilter { get; set; } = false;
    
    public string SemanticFilterPrompt { get; set; }
    public bool UseDbFilterForAntiHullicinations { get; set; } = true;

    public bool? SemanticFilterKeySort { get; set; }

    public string RelationName { get; set; }

    public string NoRowsFoundInstruction { get; set; }

    public string RowContextMessage { get; set; }

    public string RefusalMessage { get; set; }

    [JsonInclude]
    internal JsonElement? SemanticFilterOutSchema { get; set; }

    [JsonInclude]
    internal JsonElement? IntroSchema { get; set; }

    public bool JsonEIncludeFullRows { get; set; } = true;
    public bool JsonEIncludeRestRows { get; set; } = true;

    [JsonInclude]
    internal JsonNode JsonEOutputTemplate { get; set; }

    [JsonInclude] 
    internal JsonNode SemanticSegmentMergeJsonETemplate { get; set; }

    [JsonInclude]
    internal JsonNode IntroJsonETemplate { get; set; }

    public string SemanticFilterOutToAnswerPrompt { get; set; }

    public string AnswerSmartFormat { get; set; }

    public int AnswerSmartFormatCharLimit { get; set; } = 5_000;

    public string StripRegex { get; set; }

    public bool UseMarkdownDataInjection { get; set; }

    public string SuitablePath { get; set; }

    public int RowLimit { get; set; } = 10;
    public int SemanticRowLimit { get; set; } = 10;
    public int SemanticMinSegments { get; set; } = 1;
    public int SemanticMaxSegments { get; set; } = 1;
    public string SelectFields { get; set; }

    public string KeyField { get; set; }
    public TimeSpan? AnswerLogExpiryTimeSpan { get; set; }

    public string KeyPath { get; set; }

    public const string DefaultSimilarityField = "Similarity";
    public const string DefaultFuzzyScoreField = "FuzzyScore";
    public string FuzzyScoreField { get; set; }
    public string SimilarityField { get; set; }
    public int SimilarityWeight = 1;
    public string EmbeddingModel { get; set; }

    public Dictionary<string, QueryFilterField> QueryFilterFields { get; set; } = new();
    public Dictionary<string, string> FieldMarkdownFormats { get; set; } = new();

    public bool DbFilterBeforeSemanticFilter { get; set; }

    public List<string> MarkdownLLMSkipColumns { get; set; }

    public bool UseEmbeddings { get; set; }

    public int? MaxConcurrentJsonE { get; set; }

    public bool AntiHallucinateByKey { get; set; }
    
    public bool EmbedBeforeSemanticFilter { get; set; }

    public bool DedupByKey { get; set; }
    public bool UseSmartFormat { get; set; }
    public TimeSpan? IntroTimeout { get; set; }
    public TimeSpan? IntroMinDelay { get; set; }
    public TimeSpan? IntroMaxDelay { get; set; }
    public TimeSpan? SortTimeout { get; set; }
    public TimeSpan? SortMinDelay { get; set; }
    public TimeSpan? SortMaxDelay { get; set; }
    public TimeSpan? DbFilterTimeout { get; set; }
    public TimeSpan? DbFilterMinDelay { get; set; }
    public TimeSpan? DbFilterMaxDelay { get; set; }
    public int? DbFilterMaxRetries { get; set; }
    public TimeSpan? SemanticFilterTimeout { get; set; }
    public TimeSpan? SemanticFilterMinDelay { get; set; }
    public TimeSpan? SemanticFilterMaxDelay { get; set; }
    public string[] SortPromptLines { get; set; }
    public string SortPrompt { get; set; }
    public int? SemanticFilterMaxRetries { get; set; }
    public bool UseSort { get; set; }
    public bool SortBeforeDbFilterQuery { get; set; }
    public string EmbeddingFloatArrayField { get; set; }
    public string EmbeddingJsonField { get; set; }
    public string EmbeddingJsonUpdateSql { get; set; }
    public TimeSpan? UseEmbeddingsIfComputedBy { get; set; }
    public TimeSpan? SlowThreshold { get; set; }
    public string SmartFormats { get; set; }
    public bool EnableAnswerLogging { get; set; }

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(PostgreSQLClientName))
            throw new ArgumentException(
                $"{nameof(FlatQueryAssistantOptions)}.{nameof(PostgreSQLClientName)} is required");
        
        if(string.IsNullOrWhiteSpace(AnswerLogPostgresSQLClientName))
            AnswerLogPostgresSQLClientName = PostgreSQLClientName;
        
        if (!string.IsNullOrWhiteSpace(OpenAIAgentName) && string.IsNullOrWhiteSpace(AgentName))
            AgentName = OpenAIAgentName.Contains("OpenAI") ? OpenAIAgentName : "OpenAI." + OpenAIAgentName;
        SemanticFilterAgentName = SemanticFilterAgentName.NullIfWhiteSpace() ?? AgentName;
        DbFilterAgentName = DbFilterAgentName.NullIfWhiteSpace() ?? AgentName;
        SemanticFilterOutToAnswerAgentName = SemanticFilterOutToAnswerAgentName.NullIfWhiteSpace() ?? AgentName;
        IntroAgentName = IntroAgentName.NullIfWhiteSpace() ?? AgentName;

        if (string.IsNullOrWhiteSpace(SemanticFilterAgentName))
            throw new ArgumentException($"{nameof(FlatQueryAssistantOptions)}.{nameof(SemanticFilterAgentName)} is required");

        if (string.IsNullOrWhiteSpace(DbFilterAgentName))
            throw new ArgumentException($"{nameof(FlatQueryAssistantOptions)}.{nameof(DbFilterAgentName)} is required");

        if (string.IsNullOrWhiteSpace(IntroAgentName))
            throw new ArgumentException($"{nameof(FlatQueryAssistantOptions)}.{nameof(IntroAgentName)} is required");

        if (string.IsNullOrWhiteSpace(SemanticFilterOutToAnswerAgentName))
            throw new ArgumentException($"{nameof(FlatQueryAssistantOptions)}.{nameof(SemanticFilterOutToAnswerAgentName)} is required");

        if (DbFilterPromptLines?.Length > 0)
        {
            DbFilterPrompt += ("\n" + string.Join("\n", DbFilterPromptLines)).Trim();
            DbFilterPromptLines = null;
        }

        if (SortPromptLines?.Length > 0)
        {
            SortPrompt += ("\n" + string.Join("\n", SortPromptLines)).Trim();
            SortPromptLines = null;
        }

        /*if (EnableAnswerLogging)
            AnswerLogExpiryTimeSpan = AnswerLogExpiryTimeSpan ??
                                      throw new InvalidOperationException(
                                          $"{nameof(FlatQueryAssistantOptions)}.{nameof(AnswerLogExpiryTimeSpan)} cannot be null if {nameof(EnableAnswerLogging)} is enabled");*/
        
        if(AnswerLogExpiryTimeSpan != null && EnableAnswerLogging)
            if (AnswerLogExpiryTimeSpan < TimeSpan.FromHours(1))
                throw new InvalidOperationException($"{nameof(FlatQueryAssistantOptions)}.{nameof(AnswerLogExpiryTimeSpan)} cannot be less than 1)");
        
        if (string.IsNullOrWhiteSpace(DbFilterPrompt))
            throw new InvalidOperationException(
                $"{nameof(FlatQueryAssistantOptions)}.{nameof(DbFilterPrompt)} is required");

        if (SemanticFilterPromptLines?.Length > 0)
        {
            var lines = string.Join("\n", SemanticFilterPromptLines).Trim();
            if (!(SemanticFilterPrompt?.EndsWith(lines) ?? false))
            {
                SemanticFilterPrompt += ("\n" + lines).Trim();
                SemanticFilterPromptLines = null;
            }
        }

        if (string.IsNullOrWhiteSpace(SemanticFilterPrompt))
            throw new InvalidOperationException(
                $"{nameof(FlatQueryAssistantOptions)}.{nameof(SemanticFilterPrompt)} is required");

        if (IntroPromptLines?.Length > 0)
        {
            var lines = string.Join("\n", IntroPromptLines).Trim();
            if (!(IntroPrompt?.EndsWith(lines) ?? false))
            {
                IntroPrompt += ("\n" + lines).Trim();
                IntroPromptLines = null;
            }
        }

        if (string.IsNullOrWhiteSpace(RelationName))
            throw new InvalidOperationException(
                $"{nameof(FlatQueryAssistantOptions)}.{nameof(RelationName)} is required");

        if (string.IsNullOrWhiteSpace(NoRowsFoundInstruction))
            throw new InvalidOperationException(
                $"{nameof(FlatQueryAssistantOptions)}.{nameof(NoRowsFoundInstruction)} is required");

        if (string.IsNullOrWhiteSpace(RowContextMessage))
            throw new InvalidOperationException(
                $"{nameof(FlatQueryAssistantOptions)}.{nameof(RowContextMessage)} is required");

        if (string.IsNullOrWhiteSpace(RefusalMessage))
            throw new InvalidOperationException(
                $"{nameof(FlatQueryAssistantOptions)}.{nameof(RefusalMessage)} is required");

        if (RowLimit < 1)
            throw new InvalidOperationException(
                $"{nameof(FlatQueryAssistantOptions)}.{nameof(RowLimit)} must be greater than 0");

        if (string.IsNullOrWhiteSpace(SelectFields))
            throw new InvalidOperationException($"{nameof(FlatQueryAssistantOptions)}.{nameof(SelectFields)} is required");

        if (QueryFilterFields == null)
            throw new InvalidOperationException($"{nameof(FlatQueryAssistantOptions)}.{nameof(QueryFilterFields)} is required");

        foreach (var fld in QueryFilterFields)
            fld.Value.Validate();
        
        if (AntiHallucinateByKey && string.IsNullOrWhiteSpace(KeyField))
            throw new InvalidOperationException(
                $"{nameof(FlatQueryAssistantOptions)}.{nameof(KeyField)} is required when {nameof(AntiHallucinateByKey)} is true");
        
        if (DedupByKey && string.IsNullOrWhiteSpace(KeyField))
            throw new InvalidOperationException(
                $"{nameof(FlatQueryAssistantOptions)}.{nameof(KeyField)} is required when {nameof(AntiHallucinateByKey)} is true");
 
        if (UseEmbeddings && string.IsNullOrWhiteSpace(KeyPath))
            throw new InvalidOperationException(
                $"{nameof(FlatQueryAssistantOptions)}.{nameof(KeyPath)} is required when {nameof(UseEmbeddings)} is true");
        
        if (UseEmbeddingsIfComputedBy is { TotalMilliseconds: < 1 })
            throw new InvalidOperationException(
                $"{nameof(FlatQueryAssistantOptions)}.{nameof(UseEmbeddingsIfComputedBy)} must be at least 1 millisecond");
        
        if (SemanticMaxSegments < 1)
            throw new InvalidOperationException(
                $"{nameof(FlatQueryAssistantOptions)}.{nameof(SemanticMaxSegments)} must be at least 1");
        
        if (SemanticMinSegments < 1)
            throw new InvalidOperationException(
                $"{nameof(FlatQueryAssistantOptions)}.{nameof(SemanticMinSegments)} must be at least 1");
        
        if (SemanticMaxSegments < SemanticMinSegments)
            throw new InvalidOperationException(
                $"{nameof(FlatQueryAssistantOptions)}.{nameof(SemanticMaxSegments)} must be greater than or equal to {nameof(SemanticMinSegments)}");
        
        if (SemanticMaxSegments > 1 && SemanticFilterOutSchema is not { ValueKind: JsonValueKind.Object })
            throw new InvalidOperationException(
                $"{nameof(FlatQueryAssistantOptions)}.{nameof(SemanticFilterOutSchema)} must be an object when {nameof(SemanticMaxSegments)} is greater than 1");

        if (!string.IsNullOrWhiteSpace(IntroPrompt))
        {
            if (IntroSchema is not { ValueKind: JsonValueKind.Object })
                throw new InvalidOperationException(
                    $"{nameof(FlatQueryAssistantOptions)}.{nameof(IntroSchema)} must be an object when {nameof(IntroPrompt)} is not empty");
            
            if (IntroJsonETemplate is null)
                throw new InvalidOperationException(
                    $"{nameof(FlatQueryAssistantOptions)}.{nameof(IntroJsonETemplate)} is required when {nameof(IntroPrompt)} is not empty");
        }
    }

    public FlatQueryAssistantOptions Clone() =>
        new()
        {
            PostgreSQLClientName = PostgreSQLClientName,
            SemanticFilterAgentName = SemanticFilterAgentName,
            DbFilterAgentName = DbFilterAgentName,
            DbFilterPrompt = DbFilterPrompt,
            SemanticFilterPrompt = SemanticFilterPrompt,
            SemanticFilterOutToAnswerPrompt = SemanticFilterOutToAnswerPrompt,
            RelationName = RelationName,
            NoRowsFoundInstruction = NoRowsFoundInstruction,
            RowContextMessage = RowContextMessage,
            RefusalMessage = RefusalMessage,
            SemanticFilterOutSchema = SemanticFilterOutSchema,
            IntroSchema = IntroSchema,
            JsonEOutputTemplate = JsonEOutputTemplate,
            AnswerSmartFormat = AnswerSmartFormat,
            RowLimit = RowLimit,
            SemanticRowLimit = SemanticRowLimit,
            SemanticMaxSegments = SemanticMaxSegments,
            SemanticMinSegments = SemanticMinSegments,
            SelectFields = SelectFields,
            UseMarkdownDataInjection = UseMarkdownDataInjection,
            QueryFilterFields = QueryFilterFields.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Clone()),
            KeyField = KeyField,
            KeyPath = KeyPath,
            UseEmbeddings = UseEmbeddings,
            SimilarityField = SimilarityField,
            EmbeddingModel = EmbeddingModel,
            AntiHallucinateByKey = AntiHallucinateByKey,
            DedupByKey = DedupByKey,
            SuitablePath = SuitablePath,
            SemanticFilterTimeout = SemanticFilterTimeout,
            SemanticFilterMaxDelay = SemanticFilterMaxDelay,
            SemanticFilterMinDelay = SemanticFilterMinDelay,
            SemanticFilterMaxRetries = SemanticFilterMaxRetries,
            SemanticFilterKeySort = SemanticFilterKeySort,
            DbFilterTimeout = DbFilterTimeout,
            DbFilterMaxDelay = DbFilterMaxDelay,
            DbFilterMinDelay = DbFilterMinDelay,
            DbFilterMaxRetries = DbFilterMaxRetries,
            EmbeddingJsonField = EmbeddingJsonField,
            EmbeddingJsonUpdateSql = EmbeddingJsonUpdateSql,
            EmbeddingFloatArrayField = EmbeddingFloatArrayField,
            DbFilterBeforeSemanticFilter = DbFilterBeforeSemanticFilter,
            MarkdownLLMSkipColumns = MarkdownLLMSkipColumns,
            EmbedBeforeSemanticFilter = EmbedBeforeSemanticFilter,
            UseEmbeddingsIfComputedBy = UseEmbeddingsIfComputedBy,
            SlowThreshold = SlowThreshold,
            MaxConcurrentJsonE = MaxConcurrentJsonE,
            FieldMarkdownFormats = FieldMarkdownFormats,
            SemanticFilterOutToAnswerAgentName = SemanticFilterOutToAnswerAgentName,
            SemanticSegmentMergeJsonETemplate = SemanticSegmentMergeJsonETemplate,
            SemanticFilterPredictedOutput = SemanticFilterPredictedOutput,
            SemanticFilterMaxCompletionTokens = SemanticFilterMaxCompletionTokens,
            DbFilterMaxCompletionTokens = DbFilterMaxCompletionTokens,
            DbFilterPredictedOutput = DbFilterPredictedOutput,
            AnswerSmartFormatCharLimit = AnswerSmartFormatCharLimit,
            SmartFormats = SmartFormats,
            UseSmartFormat = UseSmartFormat,
            IntroPrompt = IntroPrompt,
            IntroPromptLines = IntroPromptLines,
            IntroAgentName = IntroAgentName,
            AgentName = AgentName,
            GeminiSchemaPath = GeminiSchemaPath,
            IntroJsonETemplate = IntroJsonETemplate,
            UseDbFilter = UseDbFilter,
            JsonEIncludeFullRows = JsonEIncludeFullRows,
            SortPrompt = SortPrompt,
            SortPromptLines = SortPromptLines,
            SortBeforeDbFilterQuery = SortBeforeDbFilterQuery,
            UseSort = UseSort,
            UseChatCache = UseChatCache,
            UseDbFilterForAntiHullicinations = UseDbFilterForAntiHullicinations,
            ChatCacheCheckSeconds = ChatCacheCheckSeconds,
            ExpireOnCreationMinutes = ExpireOnCreationMinutes,
            EnableAnswerLogging = EnableAnswerLogging,
            AnswerLogExpiryTimeSpan = AnswerLogExpiryTimeSpan,
            SortMinDelay = SortMinDelay,
            SortMaxDelay = SortMaxDelay,
            IntroTimeout = IntroTimeout,
            IntroMaxDelay = IntroMaxDelay,
            IntroMinDelay = IntroMinDelay,
            SortTimeout = SortTimeout,
            UseSoftFilter = UseSoftFilter,
            SchemaSwapCompletionTimeSpan = SchemaSwapCompletionTimeSpan,
            EnableSchemaSwapping = EnableSchemaSwapping,
            EnableFilterSchemaSwapping = EnableFilterSchemaSwapping,
            EnableSortSchemaSwapping = EnableSortSchemaSwapping,
            EnableIntroSchemaSwapping = EnableIntroSchemaSwapping,
            EnableSemanticSchemaSwapping = EnableSemanticSchemaSwapping,
            EnableQueryCache = EnableQueryCache,
            SimilarityWeight = SimilarityWeight,
            AnswerLogPostgresSQLClientName = AnswerLogPostgresSQLClientName
        };
}
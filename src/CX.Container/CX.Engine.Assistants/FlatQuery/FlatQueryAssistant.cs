using System.Collections.Concurrent;
using System.Data.Common;
using System.Diagnostics;
using System.Dynamic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using CX.Engine.Assistants.ContextAI;
using CX.Engine.Assistants.SchemaSwap;
using CX.Engine.ChatAgents;
using CX.Engine.ChatAgents.Gemini;
using CX.Engine.Common;
using CX.Engine.Common.Db;
using CX.Engine.Common.Embeddings;
using CX.Engine.Common.Embeddings.OpenAI;
using CX.Engine.Common.Json;
using CX.Engine.Common.JsonSchemas;
using CX.Engine.Common.PostgreSQL;
using CX.Engine.Common.Tracing;
using CX.Engine.Common.Tracing.Langfuse;
using Flurl.Http;
using Flurl.Util;
using FuzzySharp;
using FuzzySharp.Extractor;
using FuzzySharp.SimilarityRatio.Scorer;
using JetBrains.Annotations;
using Json.JsonE;
using Json.More;
using Json.Path;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using SmartFormat;
using Process = FuzzySharp.Process;

namespace CX.Engine.Assistants.FlatQuery;

public class FlatQueryAssistant : IAssistant, IDisposable
{
    private readonly ILogger _logger;
    private readonly LangfuseService _langfuseService;
    private readonly OpenAIEmbedder _embedder;
    private readonly ContextAIService _contextAiService;
    private readonly QueryCache _queryCache;
    private readonly string _name;
    private readonly IServiceProvider _sp;
    private readonly IDisposable _optionsChangeDisposable;

    private readonly MemoryCache<JsonNode, Task<float[]>> _embeddingCache = new(new()
    {
        EntriesExpiresAfterNoAccessDuration = TimeSpan.FromHours(1),
        ExpiryCheckInterval = TimeSpan.FromMinutes(1)
    }, JsonNodeEqualityComparer.Instance);

    private readonly MemoryCache<string, AssistantAnswer> _chatCache = new(new()
    {
        EntriesExpiresAfterCreation = TimeSpan.FromMinutes(5),
        ExpiryCheckInterval = TimeSpan.FromSeconds(1)
    }, StringComparer.Ordinal);

    private readonly CancellationTokenSource _ctsDisposed = new();
    private readonly DynamicSlimLock _slimLockJsonE = new(1);
    private FlatQueryAssistantOptions _options;
    private bool _optionsExists;
    private const string SortOrderTitle = "PropertySortOrder";

    public readonly FlatQueryAssistantMetrics Metrics;
    private readonly ConcurrentDictionary<string, SchemaSwapper> _schemaSwappings = new(); 
    private SchemaSwapperOptions _schemaSwapperOptions;

    public FlatQueryAssistant(string name, IOptionsMonitor<FlatQueryAssistantOptions> options, IServiceProvider sp, ILogger logger,
        LangfuseService langfuseService, OpenAIEmbedder embedder, IConfigurationSection section, [NotNull] ContextAIService contextAiService,
        [NotNull] QueryCache queryCache)
    {
        _sp = sp ?? throw new ArgumentNullException(nameof(sp));
        _name = name ?? throw new ArgumentNullException(nameof(name));
        _langfuseService = langfuseService ?? throw new ArgumentNullException(nameof(langfuseService));
        _embedder = embedder ?? throw new ArgumentNullException(nameof(embedder));
        _contextAiService = contextAiService ?? throw new ArgumentNullException(nameof(contextAiService));
        _queryCache = queryCache ?? throw new ArgumentNullException(nameof(queryCache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _optionsChangeDisposable = options.Snapshot(section, () => _options, o =>
            {
                _options = o;
                _slimLockJsonE.SetMaxCount(_options.MaxConcurrentJsonE ?? 1);
                _chatCache.UpdateMemoryCacheOptions(new() { EntriesExpiresAfterCreation = TimeSpan.FromMinutes(_options.ChatCacheCheckSeconds ?? 1), ExpiryCheckInterval = TimeSpan.FromMinutes(_options.ExpireOnCreationMinutes ?? 5)});
                _schemaSwapperOptions = new()
                {
                    AgentName = o.AgentName, 
                    CompletionThreshold = o.SchemaSwapCompletionTimeSpan
                };
                
                foreach (var kvp in _schemaSwappings)
                    kvp.Value.UpdateOptions(_schemaSwapperOptions);
            },
            v => _optionsExists = v, _logger, sp);
        Metrics = new(_sp, name);
    }

    public class SortCondition
    {
        public string Field { get; set; }

        [Semantic(Choices = ["ASC", "DESC"])] public string SortOrder { get; set; }

        
        public static IEnumerable<SortCondition> ToSortConditions(Dictionary<string, object> dictionary)
        {
            if (dictionary is null)
                throw new ArgumentNullException(nameof(dictionary));

            if (!dictionary.TryGetValue(SortOrderTitle, out var sortOrderValue))
                throw new InvalidOperationException($"{SortOrderTitle} is missing from the dictionary.");

            // Check if sortOrderValue is a JsonArray
            if (sortOrderValue is not JsonArray sortOrderJsonArray)
                throw new InvalidOperationException($"{SortOrderTitle} is not a JsonArray.");

            // Convert JsonArray to List<string>
            var sortOrderArray = sortOrderJsonArray
                .Select(item => item?.ToString())
                .Where(item => item != null)
                .ToList();

            dictionary.Remove(SortOrderTitle);

            var orderedKeys = sortOrderArray
                .Where(dictionary.ContainsKey)
                .Concat(dictionary.Keys.Except(sortOrderArray));

            return orderedKeys.Select(key => new SortCondition
            {
                Field = key,
                SortOrder = dictionary[key]?.ToString()
            });
        }


    }

    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public class EmbeddingJson
    {
        public JsonNode RowJson { get; set; }
        public float[] Embedding { get; set; }
    }

    public class SortJsonClass 
    {
        public JsonNode Parent;
        public JsonArray Array;
        public JsonObject JsonRow;
        public JsonNode Key;
        public SortJsonClass(JsonObject jsonRow, JsonArray array = null, JsonNode parent = null, JsonNode key = null) 
        {
            JsonRow = jsonRow;
            Array = array;
            Parent = parent;
            Key = key;
        }
    }

    public class SoftFilterSort
    {
        public string Field { get; set; }
        public object Value { get; set; }
        public Func<JsonObject, SoftFilterSort, bool> Scoring { get; set; }
    }

    private string AnswerSoftFilterSort(string answer, string filterRes, Dictionary<JsonNode, JsonObject> keyedRows)
    {
        var opts = _options.Clone();
        var queryFields = opts.QueryFilterFields;
        
        var node = JsonNode.Parse(answer);
        var path = JsonPath.Parse(opts.KeyPath);
        var res = path.Evaluate(node);
        var matches = res.Matches.Select(x => x.Value).ToList();
        var scoring = new List<SoftFilterSort>();
        var filter = JsonNode.Parse(filterRes);
        
        var rowsJson = new List<SortJsonClass>();
        foreach (var match in matches)
        {
            var (parrent, arr) = match.GetParentInNearestArray();
            var jsonRow = keyedRows[match];
            if(jsonRow == null) continue;
            rowsJson.Add(new SortJsonClass(jsonRow, arr, parrent, match));
        }
        
        foreach (var field in queryFields)
        {
            var v = field.Value;
            var jsonValue = filter![field.Key]!;
            var fieldName = field.Value.FieldName;
            if (v.DataType == FlatQueryFilterFieldType.Integer)
            {
                var min = filter[field.Key + "Min"]?.GetValue<int?>();
                var max = filter[field.Key + "Max"]?.GetValue<int?>();

                if (min.HasValue && min != v.IntNotSpecifiedValue)
                {
                    scoring.Add(new SoftFilterSort() { Field = fieldName, Value = min.Value, Scoring = (field, self) =>
                    {
                        if (field[self.Field].TryGetValue<int>(out var value) && self.Value is int selfValue)
                            return selfValue > value;
                        return false;
                    }});
                }

                if (max.HasValue && max != v.IntNotSpecifiedValue)
                {
                    scoring.Add(new SoftFilterSort() { Field = fieldName, Value = min.Value, Scoring = (field, self) =>
                    {
                        if (field[self.Field].TryGetValue<int>(out var value) && self.Value is int selfValue)
                            return selfValue < value;
                        return false;
                    }});
                }
            }
            else if (v.DataType == FlatQueryFilterFieldType.Double)
            {
                var min = filter[field.Key + "Min"]?.GetValue<double?>();
                var max = filter[field.Key + "Max"]?.GetValue<double?>();

                if (min.HasValue && (!v.IntNotSpecifiedValue.HasValue || Math.Abs(min.Value - v.IntNotSpecifiedValue.Value) > 0.0001))
                {
                    scoring.Add(new SoftFilterSort() { Field = fieldName, Value = min.Value, Scoring = (field, self) =>
                    {
                        if (field[self.Field].TryGetValue<double>(out var value) && self.Value is double selfValue)
                            return selfValue > value;
                        return false;
                    }});
                }

                if (max.HasValue && (!v.IntNotSpecifiedValue.HasValue || Math.Abs(min.Value - v.IntNotSpecifiedValue.Value) > 0.0001))
                {
                    scoring.Add(new SoftFilterSort() { Field = fieldName, Value = min.Value, Scoring = (field, self) =>
                    {
                        if (field[self.Field].TryGetValue<double>(out var value) && self.Value is double selfValue)
                            return selfValue < value;
                        return false;
                    }});
                }
            }
            else if (v.AllowMultiple)
            {
                //Get a string[]
                var values = jsonValue?.AsArray().Select(jv => jv?.GetValue<string>()).ToArray();

                var hasValue = values?.Length > 0;
                if ((v.SemanticValuesAny ?? false) && ((values?.Contains("ANY") ?? false) || (values?.Contains("ALL") ?? false)))
                    hasValue = false;

                if (hasValue)
                {
                    scoring.Add(new SoftFilterSort() { Field = fieldName, Value = values, Scoring = (field, self) =>
                    {
                        if (field[self.Field].TryGetValue<string>(out var value) && self.Value is string[] selfValue)
                            return selfValue.Contains(value);
                        return false;
                    }});
                }
            }
            else
            {
                var value = jsonValue?.GetValue<string>();
                var hasValue = !string.IsNullOrWhiteSpace(value);
                if ((v.SemanticValuesAny ?? false) && value is "ANY" or "ALL")
                    hasValue = false;

                if (hasValue)
                {
                    scoring.Add(new SoftFilterSort() { Field = fieldName, Value = value, Scoring = (field, self) =>
                    {
                        if (field[self.Field].TryGetValue<string>(out var value) && self.Value is string selfValue)
                            return selfValue.Equals(value);
                        return false;
                    }});
                }
            }
        }

        if(scoring.Count == 0)
            return answer;
        
        foreach (var row in rowsJson)
        {
            var score = scoring.Sum(x => x.Scoring(row.JsonRow, x) ? 1 : 0);
            row.JsonRow["DbFilterScore"] =  (double)(score / scoring.Count);
        }
        
        foreach(var row in rowsJson)
            row.Array.Remove(row.Parent);

        var result = rowsJson.OrderBy(row => row.JsonRow["DbFilterScore"], JsonNodeComparer.Instance).ToList();

        foreach(var row in result)
            row.Array.Add(row.Parent);

        return node!.ToJsonString();
    }

    private async Task<SchemaBase> CheckSchemaSwappingAsync(string key, bool currentStageActive, SchemaBase schema)
    {
        var warmedSchema = schema;
        if (currentStageActive && _options.EnableSchemaSwapping)
        {
            await CXTrace.Current.SpanFor("schema-swapping", new
            {
                Key = key,
                CurrentKeys = _schemaSwappings.Keys,
            }).ExecuteAsync(async _ =>
            {
                var swapping = _schemaSwappings.GetOrAdd(key, _ => new(_schemaSwapperOptions, _sp, _logger));
                await swapping.SetAndWarmupSchemaAsync(schema);
            });
            
        }
        return warmedSchema;
    }

    private List<SortJsonClass> OrderJson(JsonNodeComparer comparer, Func<JsonNode, string, JsonNode> keySelector, List<SortJsonClass> rows, IEnumerable<SortCondition> sortConditions)
    {
        IOrderedEnumerable<SortJsonClass> rowsOrdered = null;
        foreach (var order in sortConditions)
        {

            if (order.SortOrder == "ASC")
            {
                if(rowsOrdered == null)
                    rowsOrdered = rows.OrderBy(x => keySelector(x.JsonRow, order.Field), comparer);
                else
                    rowsOrdered = rowsOrdered.ThenBy(x => keySelector(x.JsonRow, order.Field), comparer);
            }
            else if (order.SortOrder == "DESC")
            {
                if(rowsOrdered == null)
                    rowsOrdered = rows.OrderByDescending(x => keySelector(x.JsonRow, order.Field), comparer);
                else
                    rowsOrdered = rowsOrdered.ThenByDescending(x => keySelector(x.JsonRow, order.Field), comparer);
            }
        }

        return rowsOrdered?.ToList() ?? rows;
    }

    private string SortJson(string json, IEnumerable<SortCondition> sortConditions, List<JsonObject> jsonRows)
    {
        if (!sortConditions.Any() || jsonRows is null) return json;
        var opts = _options.Clone();
        var node = JsonNode.Parse(json)!;
        var path = JsonPath.Parse(opts.KeyPath!);
        var res = path.Evaluate(node);

        var matches = res.Matches.Select(x => x.Value).ToList();
        var rows = new List<SortJsonClass>();
        foreach (var match in matches) 
        {
            var key = match;
            var (parrent, arr) = match.GetParentInNearestArray();
            var jsonRow = jsonRows.FirstOrDefault(x => JsonNodeEqualityComparer.Instance.Equals(x[opts.KeyField], match));
            if(jsonRow == null) continue;
            rows.Add(new SortJsonClass(jsonRow, arr, parrent, key));
        }
        
        foreach(var row in rows)
            row.Array.Remove(row.Parent);

        var result = OrderJson(JsonNodeComparer.Instance, (row, field) => row[field], rows, sortConditions); 

        foreach(var row in result)
            row.Array.Add(row.Parent);

        return node.ToJsonString();
    }

    async Task<float[]> GetRowEmbeddingsAsync(FlatQueryAssistantOptions opts, PostgreSQLClient client, JsonNode key, JsonNode row)
    {
        _logger.LogTrace($"Getting row embedding for {key} and row exists: {_embeddingCache.TryGetValue(row, out _)}");
        var task = _embeddingCache.GetOrAdd(row, async node =>
        {
            async void FireAndForgetUpdateDb(float[] embedding)
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(opts.KeyField))
                        return;

                    if (string.IsNullOrWhiteSpace(opts.EmbeddingJsonUpdateSql))
                        return;

                    var key = row[opts.KeyField];
                    var cmd = new NpgsqlCommand();
                    cmd.CommandText = opts.EmbeddingJsonUpdateSql;
                    cmd.Parameters.AddWithValue("id", key.ToPrimitive() ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("json", JsonSerializer.Serialize(new EmbeddingJson { RowJson = row, Embedding = embedding }));
                    await client.ExecuteAsync(cmd);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error updating embeddings JSON field");
                }
            }

            try
            {
                _logger.LogTrace($"Getting row embedding for {key} doesn't exist {node}");
                var res = await _embedder.GetAsync(opts.EmbeddingModel ?? OpenAIEmbedder.Models.text_embedding_ada_002, row.ToJsonString());
                var embedding = res.Data[0].Embedding.ToArray();
                FireAndForgetUpdateDb(embedding);
                return embedding;
            }
            catch
            {
                _embeddingCache.TryRemove(row, out var _);
                throw;
            }
        });

        if (task.IsCompleted)
            return task.Result;

        return await CXTrace.Current.SpanFor(key?.ToString() ?? "no-key", new { Row = row.ToJsonString(), Exists = _embeddingCache.TryGetValue(row, out _) }).ExecuteAsync(async _ => await task);
    }

    async Task<List<JsonObject>> RowEmbeddingStageInternalAsync(
    FlatQueryAssistantOptions opts, 
    PostgreSQLClient client, 
    List<JsonObject> rowsJson,
    Task<float[]> taskQuestionEmbeddings)
{
    // Wait for the question embeddings to be available.
    float[] questionEmbeddings = await taskQuestionEmbeddings;

    // Create a working copy of the rows.
    var workingSet = rowsJson.Select(n => n.DeepClone()).ToList();

    // Define a helper function to compute (or retrieve) the row embeddings,
    // then update the row’s similarity field.
    async Task ComputeOrGetCachedEmbeddingsForRowAsync(JsonNode row)
    {
        // Retrieve any existing similarity score (or default to 0).
        string simField = opts.SimilarityField ?? FlatQueryAssistantOptions.DefaultSimilarityField;
        double existingScore = 0d;
        if (row[simField]?.ToPrimitive() is double current)
            existingScore = current;

        // Remove the current similarity field from the row.
        row.AsObject().Remove(simField);

        // Retrieve the row embeddings (this call may return cached results).
        var keyNode = opts.KeyField != null ? row[opts.KeyField] : null;
        var rowEmbeddings = await GetRowEmbeddingsAsync(opts, client, keyNode, row);

        // Compute the cosine similarity between the row and the question embeddings.
        double similarity = Math.Round(rowEmbeddings.GetCosineSimilarity(questionEmbeddings), 4);

        // Combine the previously computed score with the new similarity.
        // The formula below is taken from your original code.
        row[simField] = (existingScore + similarity * opts.SimilarityWeight * 100) / 2;
    }

        
    // Run the embedding computation concurrently for all rows.
    await CXTrace.Current.SpanFor("row-embeddings", new { Count = rowsJson.Count })
        .ExecuteAsync(async _ =>
        {
            await Task.WhenAll(workingSet.Select(row => ComputeOrGetCachedEmbeddingsForRowAsync(row!)));
        });
    
    
    // Sort the working set by the similarity field (descending order)
    string simFieldKey = opts.SimilarityField ?? FlatQueryAssistantOptions.DefaultSimilarityField;
    var newRowsJson = workingSet
        .OrderByDescending(r => r[simFieldKey]!.GetValue<double>())
        .Select(r => (JsonObject)r.DeepClone())
        .ToList();

    return newRowsJson;
}

    async Task<List<JsonObject>> RowEmbeddingStageAsync(FlatQueryAssistantOptions opts, PostgreSQLClient client, Stopwatch sw, List<JsonObject> rowsJson,
        Task<float[]> taskQuestionEmbeddings)
    {
        if (!opts.UseEmbeddings || !(rowsJson?.Count > 0))
            return rowsJson;

        Metrics.Question_Embeddings.Inc();
        var taskRowEmbeddingStage = RowEmbeddingStageInternalAsync(opts, client, rowsJson, taskQuestionEmbeddings);

        if (!opts.UseEmbeddingsIfComputedBy.HasValue)
            return await taskRowEmbeddingStage;

        var taskTimeout = sw.GetTaskForElapsedAt(opts.UseEmbeddingsIfComputedBy.Value);
        await Task.WhenAny(taskRowEmbeddingStage, taskTimeout);

        if (taskRowEmbeddingStage.IsCompletedSuccessfully)
        {
            return taskRowEmbeddingStage.Result;
        }
        else
        {
            Metrics.Question_Embeddings_TooLate.Inc();
            return rowsJson;
        }
    }
    
    private class SortStageResponse
    {
        public SortCondition[] Condition;
        public ChatResponseBase SortRes;
        public string FieldRules;
        public bool IsRefusal;
    }
    
    private class DbQueryStageResponse
    {
        public List<JsonObject> RowsJson;
        public string FieldRules;
        public ChatResponseBase FilterRes;
        public bool IsRefusal;
        public string FilterJson;
    }

    private static Dictionary<JsonNode, JsonObject> BuildKeyedRows(List<JsonObject> rows, string keyField)
    {
        var keyedRows = new Dictionary<JsonNode, JsonObject>(JsonNodeEqualityComparer.Instance);
        if (string.IsNullOrWhiteSpace(keyField))
            return keyedRows;

        foreach (var row in rows)
            if (row[keyField] != null)
                keyedRows[row[keyField]] = row;

        return keyedRows;
    }

    private async Task<SortStageResponse> DbSortStageAsync(string question, List<ChatMessage> history, FlatQueryAssistantOptions opts, SchemaResponseFormat responseFormatBase)
    {
        var agent = _sp.GetRequiredNamedService<IChatAgent>(opts.AgentName);
        var res = new SortStageResponse();

        return await CXTrace.Current.SpanFor("sort", new
        {
            Question = question
        }).ExecuteAsync(async _ =>
        {
            var sortReq = agent.GetRequest();
            
            sortReq.Question = question;
            sortReq.History.AddRange(history);
            sortReq.MaxCompletionTokens = opts.DbFilterMaxCompletionTokens;
            sortReq.PredictedOutput = opts.DbFilterPredictedOutput;
            var sortSchema = agent.GetSchema(_name);
            sortSchema.Object.AddProperty("Reasoning", PrimitiveTypes.String);

            var sbFieldRules = new StringBuilder();

            Task ProcessFieldAsync(KeyValuePair<string, QueryFilterField> field)
            {
                var v = field.Value;

                if (!v.AllowSort)
                {
                    return Task.CompletedTask;
                }

                if (sortSchema.Object.Properties.Any(kvp =>
                        string.Equals(kvp.Key, field.Key, StringComparison.InvariantCultureIgnoreCase)))
                    throw new InvalidOperationException($"Duplicate field name encountered: {field.Key}");

                var choices = new List<string>(["ASC", "DESC", "NONE"]);

                if (!string.IsNullOrWhiteSpace(v.FieldRules))
                    lock (sbFieldRules)
                        sbFieldRules.AppendLine(
                            $"- {field.Value.FieldName} {(!string.IsNullOrWhiteSpace(field.Value.SortRules) ? "- " + field.Value.SortRules : "")}");

                lock (sortSchema)
                    sortSchema.Object.AddProperty(field.Value.FieldName, PrimitiveTypes.String, choices: choices,
                        itemType: PrimitiveTypes.String);


                return Task.CompletedTask;
            }

            await (from field in opts.QueryFilterFields select ProcessFieldAsync(field));
            sortSchema.Object.AddProperty(SortOrderTitle, PrimitiveTypes.Array,
                choices: sortSchema.Object.Properties.Keys.Where(x => x != "Reasoning").ToList(),
                itemType: PrimitiveTypes.String);

            res.FieldRules = sbFieldRules.ToString();

            {
                dynamic formatContext = new ExpandoObject();
                formatContext.fieldRules = res.FieldRules;
                sortReq.SystemPrompt = Smart.Format(opts.SortPrompt, formatContext);
            }
            
            sortSchema = await CXTrace.Current.SpanFor("sort-schema-swapping", new {Schema = sortSchema}).ExecuteAsync(async _ => await CheckSchemaSwappingAsync("sort", opts.EnableSortSchemaSwapping, sortSchema));
            
            sortReq.SetResponseSchema(sortSchema);
            sortReq.TimeOut = opts.DbFilterTimeout;
            sortReq.MinDelay = opts.DbFilterMinDelay;
            sortReq.MaxDelay = opts.DbFilterMaxDelay;
            sortReq.MaxRetries = opts.DbFilterMaxRetries;
            res.SortRes = await agent.RequestAsync(sortReq);
            if(res.SortRes?.IsRefusal ?? false)
            {
                if (responseFormatBase != null)
                    throw new AgentRefusalException($"The agent has refused to respond to the DB filter operation: {res.SortRes.Answer}");

                res.IsRefusal = true;
                return res;
            }
            
            var sort = JsonSerializer.Deserialize<JsonNode>(res.SortRes!.Answer);
            //var sortConditions = filter["SortBy"].Deserialize<SortCondition[]>();
            
            var sortJson = JsonSerializer.Serialize(sort);
            CXTrace.Current.Event("Sort", "Sort computed", CXTrace.ObservationLevel.DEFAULT, sortJson);
            if (_logger.IsEnabled(LogLevel.Trace))
                _logger.LogTrace("Sort: {Sort}", sortJson);

            var sortPropertiesOrder = sort.ToKeyValuePairs().Where(x => x.Key != "Reasoning")
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            res.Condition = SortCondition.ToSortConditions(sortPropertiesOrder).ToArray();
            return res;
        });
    }
            
    private async Task<DbQueryStageResponse> DbFilterStageAsync(
        string question, List<ChatMessage> history, PostgreSQLClient client, FlatQueryAssistantOptions opts,
        SchemaResponseFormat responseFormatBase, Task<SortStageResponse> taskSortStage)
    {
        var res = new DbQueryStageResponse();

        if (!opts.UseDbFilter && !opts.UseDbFilterForAntiHullicinations)
            return res;

        var agent = _sp.GetRequiredNamedService<IChatAgent>(opts.AgentName);

        return await CXTrace.Current.SpanFor("db-filter", new
        {
            Question = question
        }).ExecuteAsync(async span =>
        {
            var filterReq = agent.GetRequest();
            filterReq.Question = question;
            filterReq.History.AddRange(history);
            filterReq.MaxCompletionTokens = opts.DbFilterMaxCompletionTokens;
            filterReq.PredictedOutput = opts.DbFilterPredictedOutput;
            var filterSchema = agent.GetSchema(_name);
            filterSchema.Object.AddProperty("Reasoning", PrimitiveTypes.String);
            filterSchema.Object.AddProperty("SearchDatabase", PrimitiveTypes.Boolean);

            var sbFieldRules = new StringBuilder();

            async Task ProcessFieldAsync(KeyValuePair<string, QueryFilterField> field)
            {
                switch (field.Key)
                {
                    case "SearchDatabase" or "RowLimit":
                        throw new InvalidOperationException($"{field.Key} is a reserved field name");
                }

                if (filterSchema.Object.Properties.Any(kvp => string.Equals(kvp.Key, field.Key, StringComparison.InvariantCultureIgnoreCase)))
                    throw new InvalidOperationException($"Duplicate field name encountered: {field.Key}");

                List<string> choices = null;
                var v = field.Value;

                var type = PrimitiveTypes.String;
                switch (v.DataType)
                {
                    case FlatQueryFilterFieldType.Array:
                        type = PrimitiveTypes.Array;
                        break;
                    case FlatQueryFilterFieldType.Double:
                        type = PrimitiveTypes.Number;
                        break;
                    case FlatQueryFilterFieldType.Integer:
                        type = PrimitiveTypes.Integer;
                        break;
                }
                string itemType = null;

                //String only
                if (v.AllowMultiple)
                {
                    itemType = type;
                    type = PrimitiveTypes.Array;
                }

                if (v.DataType == FlatQueryFilterFieldType.Array)
                {
                    choices = opts.EnableQueryCache ? await _queryCache.ListAsync(client, v.SemanticValuesQuery!, r => r.IsDBNull(0) ? null : r.GetString(0)) : await client.ListStringAsync(v.SemanticValuesQuery!);
                    
                    if ((v.SemanticValuesAny ?? false) && !choices.Contains("ANY"))
                        choices.Add("ANY");

                    if ((v.SemanticValuesAny ?? false) && !choices.Contains("ALL"))
                        choices.Add("ALL");
                    
                    itemType = PrimitiveTypes.String;
                }
                //String only
                else if (!string.IsNullOrWhiteSpace(v.SemanticValuesQuery))
                {
                    choices = opts.EnableQueryCache ? await _queryCache.ListAsync(client, v.SemanticValuesQuery!, r => r.IsDBNull(0) ? null : r.GetString(0)) : await client.ListStringAsync(v.SemanticValuesQuery!);

                    if (choices.Contains(null))
                        throw new InvalidOperationException($"Semantic values query for field {field.Key} returned null");

                    if ((v.SemanticValuesAny ?? false) && !choices.Contains("ANY"))
                        choices.Add("ANY");

                    if ((v.SemanticValuesAny ?? false) && !choices.Contains("ALL"))
                        choices.Add("ALL");
                }

                if (!string.IsNullOrWhiteSpace(v.FieldRules))
                    lock (sbFieldRules)
                        sbFieldRules.AppendLine($"- {field.Key}: {Smart.Format(v.FieldRules, choices ?? [])}");

                lock (filterSchema)
                    if (v.DataType == FlatQueryFilterFieldType.Integer || v.DataType == FlatQueryFilterFieldType.Double)
                    {
                        if (filterSchema.Object.HasPropertyCI(field.Key + "Min") || filterSchema.Object.HasPropertyCI(field.Key + "Max"))
                            throw new InvalidOperationException(
                                $"Cannot add range parameters for field: {field.Key} since {field.Key}Min or {field.Key}Max already defined.");

                        filterSchema.Object.AddProperty(field.Key + "Min", type, choices: choices!, itemType: itemType);
                        filterSchema.Object.AddProperty(field.Key + "Max", type, choices: choices!, itemType: itemType);
                    }
                    else
                    {
                        filterSchema.Object.AddProperty(field.Key, type, choices: choices!, itemType: itemType);
                    }
            }

            await (from field in opts.QueryFilterFields select ProcessFieldAsync(field));

            res.FieldRules = sbFieldRules.ToString();

            {
                dynamic formatContext = new ExpandoObject();
                formatContext.fieldRules = res.FieldRules;
                filterReq.SystemPrompt = Smart.Format(opts.DbFilterPrompt, formatContext);
            }
            
            var warmedSchema = await CXTrace.Current.SpanFor("db-filter-schema-swapping", new {Schema = filterSchema}).ExecuteAsync(async _ => await CheckSchemaSwappingAsync("db-filter", opts.EnableSchemaSwapping, filterSchema));
            
            filterReq.SetResponseSchema(warmedSchema);
            filterReq.TimeOut = opts.DbFilterTimeout;
            filterReq.MinDelay = opts.DbFilterMinDelay;
            filterReq.MaxDelay = opts.DbFilterMaxDelay;
            filterReq.MaxRetries = opts.DbFilterMaxRetries;
            res.FilterRes = await agent.RequestAsync(filterReq);
            if (res.FilterRes?.IsRefusal ?? false)
            {
                if (responseFormatBase != null)
                    throw new AgentRefusalException($"The agent has refused to respond to the DB filter operation: {res.FilterRes.Answer}");

                res.IsRefusal = true;
                return res;
            }

            var filter = JsonSerializer.Deserialize<JsonNode>(res.FilterRes!.Answer);
            //var sortConditions = filter["SortBy"].Deserialize<SortCondition[]>();
            var searchDatabase = filter == null || filter["SearchDatabase"]!.GetValue<bool>();
            filter!["RowLimit"] = opts.RowLimit;
            filter["Reasoning"] = null;
            filter["SearchDatabase"] = null;

            var filterJson = JsonSerializer.Serialize(filter);
            CXTrace.Current.Event("Filter", "Filter computed", CXTrace.ObservationLevel.DEFAULT, filterJson);
            if (_logger.IsEnabled(LogLevel.Trace))
                _logger.LogTrace("Filter: {Filter}", filterJson);

            res.FilterJson = filterJson;

            if (searchDatabase)
            {
                var cmd = new NpgsqlCommand();
                var sql = new StringBuilder();
                sql.AppendLine($"SELECT {opts.SelectFields} FROM {opts.RelationName}");
                sql.AppendLine("WHERE (1 = 1)");

                var argNo = 1;
                foreach (var field in opts.QueryFilterFields)
                {
                    var v = field.Value;
                    var jsonValue = filter[field.Key];
                    if(v.AllowFuzzyMatching)
                        continue;
                    if (v.DataType == FlatQueryFilterFieldType.Integer)
                    {
                        var min = filter[field.Key + "Min"]?.GetValue<int?>();
                        var max = filter[field.Key + "Max"]?.GetValue<int?>();

                        if (min.HasValue && min != v.IntNotSpecifiedValue)
                        {
                            sql.AppendLine($"AND ({v.FieldName} >= @arg{argNo})");
                            cmd.Parameters.AddWithValue("arg" + argNo, min!);
                            argNo++;
                        }

                        if (max.HasValue && max != v.IntNotSpecifiedValue)
                        {
                            sql.AppendLine($"AND ({v.FieldName} <= @arg{argNo})");
                            cmd.Parameters.AddWithValue("arg" + argNo, max!);
                            argNo++;
                        }
                    }
                    else if (v.DataType == FlatQueryFilterFieldType.Double)
                    {
                        var min = filter[field.Key + "Min"]?.GetValue<double?>();
                        var max = filter[field.Key + "Max"]?.GetValue<double?>();

                        if (min.HasValue && (!v.IntNotSpecifiedValue.HasValue || Math.Abs(min.Value - v.IntNotSpecifiedValue.Value) > 0.0001))
                        {
                            sql.AppendLine($"AND ({v.FieldName} >= @arg{argNo})");
                            cmd.Parameters.AddWithValue("arg" + argNo, min!);
                            argNo++;
                        }

                        if (max.HasValue && (!v.IntNotSpecifiedValue.HasValue || Math.Abs(max.Value - v.IntNotSpecifiedValue.Value) > 0.0001))
                        {
                            sql.AppendLine($"AND ({v.FieldName} <= @arg{argNo})");
                            cmd.Parameters.AddWithValue("arg" + argNo, max!);
                            argNo++;
                        }
                    }
                    else if (v.DataType == FlatQueryFilterFieldType.Array)
                    {
                        //Get a string[]
                        var values = jsonValue?.AsArray().Select(jv => jv?.GetValue<string>()).ToArray();

                        var hasValue = values?.Length > 0;
                        if ((v.SemanticValuesAny ?? false) && ((values?.Contains("ANY") ?? false) || (values?.Contains("ALL") ?? false)))
                            hasValue = false;

                        if (hasValue)
                        {
                            sql.AppendLine($"AND {v.FieldName} ?| array['{string.Join("', '", values!)}']");
                            cmd.Parameters.AddWithValue("arg" + argNo, values!);
                            argNo++;
                        }
                    }
                    else if (v.AllowMultiple)
                    {
                        //Get a string[]
                        var values = jsonValue?.AsArray().Select(jv => jv?.GetValue<string>()).ToArray();

                        var hasValue = values?.Length > 0;
                        if ((v.SemanticValuesAny ?? false) && ((values?.Contains("ANY") ?? false) || (values?.Contains("ALL") ?? false)))
                            hasValue = false;

                        if (hasValue)
                        {
                            sql.AppendLine($"AND ({v.FieldName} = ANY(@arg{argNo}))");
                            cmd.Parameters.AddWithValue("arg" + argNo, values!);
                            argNo++;
                        }
                    }
                    else
                    {
                        var value = jsonValue?.GetValue<string>();
                        var hasValue = !string.IsNullOrWhiteSpace(value);
                        if ((v.SemanticValuesAny ?? false) && value is "ANY" or "ALL")
                            hasValue = false;

                        if (hasValue)
                        {
                            sql.AppendLine($"AND ({v.FieldName} = @arg{argNo})");
                            cmd.Parameters.AddWithValue("arg" + argNo, value!);
                            argNo++;
                        }
                    }
                }

                if (!opts.SortBeforeDbFilterQuery && taskSortStage != null)
                {
                    var sortStageReponse = await taskSortStage;
                    var sortConditions = sortStageReponse.Condition.Where(x => x.SortOrder != "NONE");
                    if(sortConditions.Any())
                        sql.AppendLine($"ORDER BY {string.Join(", ", sortConditions.Select(x => $"{x.Field} {x.SortOrder}"))}");
                }

                sql.AppendLine($"LIMIT {opts.RowLimit}");

                cmd.CommandText = sql.ToString();

                res.RowsJson = await CXTrace.Current.SpanFor("db-query-and-serialize", new
                {
                    Sql = cmd.CommandText
                }).ExecuteAsync(async span =>
                {
                    var hasEmbeddingsJsonField = !string.IsNullOrWhiteSpace(opts.EmbeddingJsonField);
                    var hasEmbeddingsFloatArrayField = !string.IsNullOrWhiteSpace(opts.EmbeddingFloatArrayField);
                    var skipColumns = new List<string>();
                    if (hasEmbeddingsJsonField)
                        skipColumns.Add(opts.EmbeddingJsonField);
                    if (hasEmbeddingsFloatArrayField)
                        skipColumns.Add(opts.EmbeddingFloatArrayField);

                    var rows = await client.ListAsync(cmd, row =>
                    {
                        var rowJson = row.ToJsonString(skipColumns);
                        var node = (JsonObject)JsonNode.Parse(rowJson)!;

                        var loadedEmbeddings = false;
                        if (hasEmbeddingsFloatArrayField)
                        {
                            var embeddings = row.GetNullable<float[]>(opts.EmbeddingFloatArrayField);
                            if (embeddings != null)
                            {
                                _embeddingCache[node] = Task.FromResult(embeddings);
                                loadedEmbeddings = true;
                            }
                        }

                        if (!loadedEmbeddings && hasEmbeddingsJsonField)
                        {
                            var json = row.GetNullable<string>(opts.EmbeddingJsonField);
                            if (json != null)
                            {
                                try
                                {
                                    var val = JsonSerializer.Deserialize<EmbeddingJson>(json);
                                    _embeddingCache[val.RowJson] = Task.FromResult(val.Embedding);
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogWarning(ex, "Error deserializing embedding JSON field");
                                }
                            }
                        }

                        return node;
                    });
                    span.Output = new { Rows = rows.Count };
                    return rows;
                });
            }

            span.Output = new
            {
                SearchDatabase = searchDatabase,
                RowCount = res.RowsJson?.Count,
                NoRowsFoundInstruction = opts.NoRowsFoundInstruction
            };
            return res;
        });
    }

    private async Task<DbQueryStageResponse> DbDefaultStageAsync(
        string question, PostgreSQLClient client, FlatQueryAssistantOptions opts)
    {
        var res = new DbQueryStageResponse();

        return await CXTrace.Current.SpanFor("db-default", new
        {
            Question = question
        }).ExecuteAsync(async span =>
        {
            res.FilterRes = null;
            res.FilterJson = "{}";

            if (true)
            {
                var cmd = new NpgsqlCommand();
                var sql = new StringBuilder();
                sql.AppendLine($"SELECT {opts.SelectFields} FROM {opts.RelationName}");
                sql.AppendLine($"LIMIT {opts.RowLimit}");

                cmd.CommandText = sql.ToString();

                res.RowsJson = await CXTrace.Current.SpanFor("db-query-and-serialize", new
                {
                    Sql = cmd.CommandText
                }).ExecuteAsync(async span =>
                {
                    var hasEmbeddingsJsonField = !string.IsNullOrWhiteSpace(opts.EmbeddingJsonField);
                    var hasEmbeddingsFloatArrayField = !string.IsNullOrWhiteSpace(opts.EmbeddingFloatArrayField);
                    var skipColumns = new List<string>();
                    if (hasEmbeddingsJsonField)
                        skipColumns.Add(opts.EmbeddingJsonField);
                    if (hasEmbeddingsFloatArrayField)
                        skipColumns.Add(opts.EmbeddingFloatArrayField);

                    var sw = Stopwatch.StartNew();
                    long embeddingFloatTicks = 0;
                    long embeddingJsonTicks = 0;
                    long rowJsonTicks = 0;

                    Func<string, Func<DbDataReader, JsonObject>, Task<List<JsonObject>>> queryFunc =
                        opts.EnableQueryCache
                            ? (command, reader) => _queryCache.ListAsync(client, command, reader)
                            : (command, reader) => client.ListAsync(command, reader);
                    
                    var rows = await queryFunc(cmd.CommandText, row =>
                    {
                        var rowJsonStart = sw.Elapsed.Ticks;
                        var rowJson = row.ToJsonString(skipColumns);
                        var node = (JsonObject)JsonNode.Parse(rowJson)!;
                        rowJsonTicks += sw.Elapsed.Ticks - rowJsonStart;

                        var loadedEmbeddings = false;
                        if (hasEmbeddingsFloatArrayField)
                        {
                            var start = sw.Elapsed.Ticks;
                            var embeddings = row.GetNullable<float[]>(opts.EmbeddingFloatArrayField);
                            if (embeddings != null)
                            {
                                _embeddingCache[node] = Task.FromResult(embeddings);
                                loadedEmbeddings = true;
                            }

                            embeddingFloatTicks += sw.Elapsed.Ticks - start;
                        }

                        if (!loadedEmbeddings && hasEmbeddingsJsonField)
                        {
                            var start = sw.Elapsed.Ticks;
                            var json = row.GetNullable<string>(opts.EmbeddingJsonField);
                            if (json != null)
                            {
                                try
                                {
                                    var val = JsonSerializer.Deserialize<EmbeddingJson>(json);
                                    _embeddingCache[val.RowJson] = Task.FromResult(val.Embedding);
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogWarning(ex, "Error deserializing embedding JSON field");
                                }
                            }

                            embeddingJsonTicks += sw.Elapsed.Ticks - start;
                        }

                        return node;
                    });
                    span.Output = new
                    {
                        Rows = rows.Count,
                        TotalTicks = sw.Elapsed.Ticks,
                        EmbeddingFloatTicks = embeddingFloatTicks,
                        EmbeddingJsonTicks = embeddingJsonTicks,
                        RowJsonTicks = rowJsonTicks
                    };
                    return rows;
                });
            }

            span.Output = new
            {
                RowCount = res.RowsJson?.Count,
                NoRowsFoundInstruction = opts.NoRowsFoundInstruction
            };
            return res;
        });
    }

    private async Task<List<JsonObject>> FuzzySort(
    Task<DbQueryStageResponse> filterStage, 
    List<JsonObject> rowsJson, 
    FlatQueryAssistantOptions opts)
    {
        if(rowsJson is null)
            return null;
        
        // Work on the list of rows.
        var res = rowsJson.Select(x => x.DeepClone().AsObject()).ToList();
        
        // If none of the fields have fuzzy matching enabled, return early.
        if (!opts.QueryFilterFields.Any(kv => kv.Value.AllowFuzzyMatching))
            return res;
        
        // Wait for the filtering stage and parse its filter JSON.
        var dbFilterResult = await filterStage;
        var filterJson = JsonNode.Parse(dbFilterResult.FilterJson)!;

        // Define a helper to process one filter field.
        void ProcessField(KeyValuePair<string, QueryFilterField> field)
        {
            var v = field.Value;
            if (!v.AllowFuzzyMatching)
                return;

            // Get the JSON node for this field; if it isn’t an array, skip it.
            var node = filterJson[field.Key];
            if (node is not JsonArray fuzzyValues)
                return;

            // Process each fuzzy search value.
            foreach (var fuzzyValue in fuzzyValues)
            {
                // Convert to a primitive value (using your own extension/helper)
                var termObj = fuzzyValue.ToPrimitive();
                if (termObj is not string pv)
                    continue;

                // Prepare a list for the extraction results.
                List<ExtractedResult<string>> scores = new List<ExtractedResult<string>>();

                // Set up the default Fuzz function.
                Func<string, string, int> fuzzFunc = Fuzz.PartialRatio;
                // Here, processFunc is assigned to the FuzzySharp extraction function.
                Func<string, IEnumerable<string>, Func<string, string>, IRatioScorer, int, IEnumerable<ExtractedResult<string>>>
                    processFunc = Process.ExtractAll;

                // Choose the appropriate scoring function based on the field’s FuzzyFunction.
                switch (v.FuzzyFunction)
                {
                    case FuzzyFunctionType.PartialRatio:
                    case FuzzyFunctionType.WeightedRatio:
                        fuzzFunc = Fuzz.WeightedRatio;
                        break;
                    case FuzzyFunctionType.Ratio:
                        fuzzFunc = Fuzz.Ratio;
                        break;
                    case FuzzyFunctionType.TokenSetRatio:
                        fuzzFunc = Fuzz.TokenSetRatio;
                        break;
                    case FuzzyFunctionType.Contains:
                        fuzzFunc = (s1, s2) => s2.Contains(s1) ? 100 : 0;
                        break;
                    case FuzzyFunctionType.Equals:
                        fuzzFunc = (s1, s2) => s2.Equals(s1) ? 100 : 0;
                        break;
                    case FuzzyFunctionType.ExtractSorted:
                        processFunc = Process.ExtractSorted;
                        break;
                    case FuzzyFunctionType.PartialTokenSetRatio:
                        fuzzFunc = Fuzz.PartialTokenSetRatio;
                        break;
                    // If needed, add more cases.
                }

                // Extract the values for this field from all rows.
                var items = res.Select(x => (x[v.FieldName]?.ToPrimitive() as string) ?? string.Empty)
                               .ToList();

                // If the FuzzyFunction indicates one of the Process.Extract functions, use it.
                if (v.FuzzyFunction < FuzzyFunctionType.ProcessFuncs)
                {
                    scores = processFunc(pv, items, null, null, 0).ToList();
                }
                else
                {
                    // Otherwise, score each row individually using our selected fuzzFunc.
                    var temp = new List<ExtractedResult<string>>();
                    for (int i = 0; i < res.Count; i++)
                    {
                        // Retrieve the field’s value from the row (or an empty string if missing).
                        var fieldValue = res[i][v.FieldName]?.ToPrimitive() as string ?? string.Empty;
                        var scoreValue = fuzzFunc(pv, fieldValue);
                        temp.Add(new ExtractedResult<string>(fieldValue, scoreValue, i));
                    }
                    scores = temp;
                }

                // Add the weighted fuzzy scores to each row.
                foreach (var score in scores)
                {
                    double finalScore = score.Score * v.FuzzyWeight;
                    // Determine the key to use (either the custom one or the default)
                    string fuzzyScoreKey = opts.FuzzyScoreField ?? FlatQueryAssistantOptions.DefaultFuzzyScoreField;

                    // Try to retrieve the current fuzzy score from the row.
                    double currentScore = 0;
                    if (res[score.Index].TryGetPropertyValue(fuzzyScoreKey, out JsonNode existingNode))
                    {
                        var existingPrimitive = existingNode?.ToPrimitive();
                        if (existingPrimitive is double d)
                            currentScore = d;
                    }
                    // Set the new fuzzy score.
                    res[score.Index][fuzzyScoreKey] = finalScore + currentScore;
                }
            }
        }

        // Process each field that is eligible for fuzzy matching.
        foreach (var field in opts.QueryFilterFields)
            ProcessField(field);
        
        // Copy the computed fuzzy score to the similarity field and then remove the temporary fuzzy score.
        foreach (var row in res)
        {
            string fuzzyScoreKey = opts.FuzzyScoreField ?? FlatQueryAssistantOptions.DefaultFuzzyScoreField;
            string similarityKey = opts.SimilarityField ?? FlatQueryAssistantOptions.DefaultSimilarityField;
            
            // Get the current fuzzy score.
            double? score = row[fuzzyScoreKey]?.ToPrimitive() as double?;
            // Set the similarity field.
            row[similarityKey] = score;
            // Remove the temporary fuzzy score field.
            row.Remove(fuzzyScoreKey);
        }
        
        // Sort the rows descending by the similarity field using your custom JSON node comparer.
        string sortKey = opts.SimilarityField ?? FlatQueryAssistantOptions.DefaultSimilarityField;
        res = res.OrderByDescending(
                x => x[sortKey],
                JsonNodeComparer.Instance)
             .ToList();

        return res;
    }

    public async Task<AssistantAnswer> AskAsync(string question, AgentRequest astCtx)
    {
        AssistantsSharedAsyncLocal.EnterAsk();
        
        using var askObserve = Metrics.Asks.StartObserve();

        try
        {
            var section = CXTrace.TraceOrSpan(() => new CXTrace(_langfuseService, astCtx.UserId, astCtx.SessionId)
                .WithName((astCtx.UserId + ": " + question).Preview(50))
                .WithTags(_name, "ask", "flat-query"),
                trace => trace.SpanFor("flat-query." + _name, new { Question = question }));

            if(_options.UseChatCache && _chatCache.Get(question) != default && astCtx.UserId != "keep-alive")
            {
                var chatCache = _chatCache.Get(question);
                return await section.WithInput(new { Question = question}).ExecuteAsync(async _ => {
                    await CXTrace.Current.SpanFor("from-chat-cache", new
                    {
                        AnswerLength = chatCache.Chunks.Count
                    }).ExecuteAsync(span =>
                    {
                        span.Output = chatCache;
                        return Task.CompletedTask;
                    });
                    return astCtx.Record(question, chatCache);
                });
            }
            _logger.LogDebug($"Started processing question: {question}");

            if (astCtx.EligibleForContextAi && !string.IsNullOrWhiteSpace(question))
                _contextAiService.EnqueueAndForget(new LogThreadMessageRequest(astCtx.SessionId!, "user", astCtx.UserId!,
                    question));

            SchemaResponseFormat responseFormatBase = null;
            var opts = _options.Clone();

            foreach (var over in astCtx.Overrides.OfType<FlatQueryAssistantOptionsOverrides>())
            {
                if (over.OverrideAnswerSmartFormat)
                    opts.AnswerSmartFormat = over.AnswerSmartFormat;

                if (over.OverrideResponseSchema)
                    responseFormatBase = over.ResponseFormatBase;

                if (over.OverrideSemanticFilterOutToAnswerPrompt)
                    opts.SemanticFilterOutToAnswerPrompt = over.SemanticFilterOutToAnswerPrompt;

                if (over.RowLimit.HasValue)
                    opts.RowLimit = over.RowLimit.Value;

                if (over.SemanticRowLimit.HasValue)
                    opts.SemanticRowLimit = over.SemanticRowLimit.Value;

                if (over.JsonEOutputTemplate != null)
                    opts.JsonEOutputTemplate = over.JsonEOutputTemplate;
            }

            {
                if (astCtx.Overrides.TryGet<ResponseFormatOverride>(out var over))
                {
                    if (over.ResponseFormat != null)
                    {
                        opts.AnswerSmartFormat = null;
                        opts.SemanticFilterOutToAnswerPrompt = "Translate this structured data to the output format.";
                    }

                    responseFormatBase = over.ResponseFormat;
                }
            }

            var client = _sp.GetRequiredNamedService<PostgreSQLClient>(_options.PostgreSQLClientName);

            return await section
                .WithInput(new
                {
                    Question = question,
                    History = astCtx.History,
                    Options = opts,
                    ResponseFormat = responseFormatBase
                })
                .ExecuteAsync(async _ =>
                {
                    var answer = new FlatQueryAssistantAnswer();
                    dynamic formatContext = new ExpandoObject();

                    ChatResponseBase filterRes;

                    #region Embedding support functions

                    async Task<float[]> GetQuestionEmbeddingsAsync() =>
                        await CXTrace.Current.SpanFor("question-embedding", new { Question = question }).ExecuteAsync(async _ =>
                        {
                            var sb = new StringBuilder();

                            if (astCtx.History.Count > 0)
                            {
                                foreach (var entry in astCtx.History)
                                {
                                    if (entry.Content != null)
                                        sb.AppendLine(entry.Role + ": " + entry.Content);
                                }

                                sb.AppendLine("User: " + question);
                            }
                            else
                                sb.AppendLine(question);

                            var res = await _embedder.GetAsync(opts.EmbeddingModel ?? OpenAIEmbedder.Models.text_embedding_ada_002, sb.ToString());
                            return res.Data[0].Embedding.ToArray();
                        });

                    #endregion

                    #region Embeddings parallel to DB Filter stage

                    var taskQuestionEmbeddings = opts.UseEmbeddings ? GetQuestionEmbeddingsAsync() : Task.FromResult(Array.Empty<float>());

                    #endregion

                    #region Intro stage

                    Task<JsonNode> taskIntroStage = null;

                    if (!string.IsNullOrEmpty(opts.IntroPrompt))
                    {
                        taskIntroStage = CXTrace.Current.SpanFor("intro", new
                        {
                            IntroPrompt = opts.IntroPrompt
                        }).ExecuteAsync(async span =>
                        {
                            var agent = _sp.GetRequiredNamedService<IChatAgent>(opts.AgentName);
                            var req = agent.GetRequest();
                            req.SystemPrompt = opts.IntroPrompt;
                            req.SetResponseSchema(opts.IntroSchema);
                            if (req is GeminiChatRequest geminiReq)
                                geminiReq.GeminiSchemaPath = opts.GeminiSchemaPath;
                            var res = await agent.RequestAsync(req);
                            if (res.IsRefusal)
                                throw new AgentRefusalException($"The agent has refused to respond to the intro operation: {res.Answer}");

                            var resE = JsonE.Evaluate(opts.IntroJsonETemplate, JsonNode.Parse(res.Answer));
                            span.Output = resE;
                            return resE;
                        });
                    }

                    #endregion
                    
                    #region Sort Stage
                    var taskSortStage = opts.UseSort ? DbSortStageAsync(question, astCtx.History, opts, responseFormatBase) : null;
                    #endregion

                    #region DB Filter/Default stage

                    var dbFilterStage = DbFilterStageAsync(question, astCtx.History, client, opts, responseFormatBase, taskSortStage);

                    List<JsonObject> rowsJson;
                    List<JsonObject> fullRowsJson;

                    {
                        var stageRes = opts.DbFilterBeforeSemanticFilter
                            ? await dbFilterStage
                            : await DbDefaultStageAsync(question, client, opts);
                        rowsJson = stageRes.RowsJson;
                        fullRowsJson = stageRes.RowsJson;
                        filterRes = stageRes.FilterRes;
                        formatContext.fieldRules = stageRes.FieldRules;
                        formatContext.filterJson = stageRes.FilterJson;

                        if (stageRes.IsRefusal)
                        {
                            section.Output = new { IsRefusal = true, Answer = opts.RefusalMessage };
                            return astCtx.Record(question, new() { Answer = opts.RefusalMessage });
                        }
                        
                    }

                    #endregion

                    #region Fuzzy Sort
                    await CXTrace.Current.SpanFor("fuzzy-sort", new
                    {
                        BeforeFuzzy = rowsJson.Select(x => x.DeepClone()[opts.KeyField]).Take(10)
                    }).ExecuteAsync(async _ =>
                    {
                        rowsJson = await FuzzySort(dbFilterStage, rowsJson, opts);
                        return rowsJson?.Select(x => x.DeepClone()[opts.KeyField]).Take(10);
                    });
                    #endregion
                    
                    #region Row Embeddings stage

                    var taskRowEmbeddingStage = RowEmbeddingStageAsync(opts, client, askObserve.Stopwatch, rowsJson, taskQuestionEmbeddings);

                    if (opts.EmbedBeforeSemanticFilter)
                    {
                        rowsJson = await taskRowEmbeddingStage;
                        fullRowsJson = rowsJson;
                    }

                    #endregion

                    #region Semantic Filter Stage

                    await CXTrace.Current.SpanFor("semantic-filter", new
                    {
                        SemanticRowLimit = opts.SemanticRowLimit,
                        RowLimit = opts.RowLimit,
                        RowsFromDb = rowsJson?.Count,
                        SemanticMaxSegments = opts.SemanticMaxSegments
                    }).ExecuteAsync(async span =>
                    {
                        //Into how many segments should we split rowsJson to get all of the rows into semantic filter queries,
                        //considering the individual request row limit?
                        var fullCoverageSegments = (int)Math.Ceiling((rowsJson?.Count ?? 0.0) / opts.SemanticRowLimit);

                        //Make sure that we do not generate more segments than the maximum allowed by the configuration
                        var ways = Math.Max(Math.Min(rowsJson?.Count ?? 0, opts.SemanticMinSegments), Math.Min(opts.SemanticMaxSegments, fullCoverageSegments));
                        var isStructured = opts.SemanticFilterOutSchema is { ValueKind: JsonValueKind.Object };

                        var splitRows = rowsJson.DeterministicSplit(ways, opts.SemanticRowLimit);
                        var agent = _sp.GetRequiredNamedService<IChatAgent>(opts.AgentName);

                        Task<string> HandleSegmentAsync(int segNo, List<JsonObject> segmentRows) =>
                            CXTrace.Current.SpanFor($"segment-{segNo}", new { }).ExecuteAsync(async _ =>
                            {
                                var sb = new StringBuilder();
                                sb.AppendLine(Smart.Format(opts.RowContextMessage, formatContext));

                                if ((segmentRows?.Count ?? 0) == 0)
                                    sb.AppendLine(Smart.Format(opts.NoRowsFoundInstruction, formatContext));
                                else
                                {
                                    if (opts.UseSmartFormat)
                                    {
                                        var skipColumns = opts.MarkdownLLMSkipColumns != null ? new List<string>(opts.MarkdownLLMSkipColumns) : new();
                                        if (opts.UseEmbeddings)
                                            skipColumns.Add(opts.SimilarityField ?? FlatQueryAssistantOptions.DefaultSimilarityField);

                                        if (opts.SemanticFilterKeySort ?? !string.IsNullOrWhiteSpace(opts.KeyField))
                                            sb.AppendLine(segmentRows.OrderBy(row => row[opts.KeyField], JsonNodeComparer.Instance).ToList()
                                                .ToSmartFormattedTable(skipColumns, opts.SmartFormats));
                                        else
                                            sb.AppendLine(segmentRows.ToList().ToSmartFormattedTable(skipColumns, opts.SmartFormats));
                                    }
                                    else if (opts.UseMarkdownDataInjection)
                                    {
                                        var skipColumns = opts.MarkdownLLMSkipColumns != null ? new List<string>(opts.MarkdownLLMSkipColumns) : new();
                                        if (opts.UseEmbeddings)
                                            skipColumns.Add(opts.SimilarityField ?? FlatQueryAssistantOptions.DefaultSimilarityField);

                                        if (opts.SemanticFilterKeySort ?? !string.IsNullOrWhiteSpace(opts.KeyField))
                                            sb.AppendLine(segmentRows.OrderBy(row => row[opts.KeyField], JsonNodeComparer.Instance).ToList()
                                                .ToMarkdownTable(skipColumns, opts.FieldMarkdownFormats));
                                        else
                                            sb.AppendLine(segmentRows.ToList().ToMarkdownTable(skipColumns, opts.FieldMarkdownFormats));
                                    }
                                    else
                                    {
                                        if (opts.SemanticFilterKeySort ?? true)
                                            sb.AppendLine(new JsonArray(segmentRows.Take(opts.SemanticRowLimit)
                                                .OrderBy(row => row[opts.KeyField], JsonNodeComparer.Instance)
                                                .Select(r => r.DeepClone()).ToArray()).ToJsonString());
                                        else
                                            // ReSharper disable once CoVariantArrayConversion
                                            sb.AppendLine(new JsonArray(segmentRows.Take(opts.SemanticRowLimit).Select(r => r.DeepClone()).ToArray())
                                                .ToJsonString());
                                    }
                                }

                                
                                var semanticFilterReq = agent.GetRequest();
                                semanticFilterReq.Question = question;
                                semanticFilterReq.SystemPrompt = opts.SemanticFilterPrompt;
                                semanticFilterReq.TimeOut = opts.SemanticFilterTimeout;       
                                semanticFilterReq.MinDelay = opts.SemanticFilterMinDelay;      
                                semanticFilterReq.MaxDelay = opts.SemanticFilterMaxDelay;      
                                semanticFilterReq.MaxRetries = opts.SemanticFilterMaxRetries;    
                                semanticFilterReq.PredictedOutput = opts.SemanticFilterPredictedOutput; 
                                semanticFilterReq.MaxCompletionTokens = opts.SemanticFilterMaxCompletionTokens;
                                semanticFilterReq.SetResponseSchema(opts.SemanticFilterOutSchema);
                                if (semanticFilterReq is GeminiChatRequest geminiReq)
                                    geminiReq.GeminiSchemaPath = opts.GeminiSchemaPath;
                                
                                if (isStructured)
                                    semanticFilterReq.SetResponseSchema(opts.SemanticFilterOutSchema);
                                semanticFilterReq.History.AddRange(astCtx.History);
                                semanticFilterReq.StringContext.Add(sb.ToString());

                                var semanticFilterOut = await agent.RequestAsync(semanticFilterReq);
                                lock (answer)
                                {
                                    if (semanticFilterReq.ResponseFormatBase != null && semanticFilterOut.IsRefusal)
                                        throw new AgentRefusalException(
                                            $"The agent has refused to respond to the semantic filter operation: {semanticFilterOut.Answer}");

                                    return semanticFilterOut.Answer;
                                }
                            });

                        var segmentTasks = new List<Task<string>>();
                        for (var segNo = 0; segNo < splitRows.Length; segNo++)
                            segmentTasks.Add(HandleSegmentAsync(segNo, splitRows[segNo]));

                        try
                        {
                            await Task.WhenAll(segmentTasks);
                        }
                        catch
                        {
                            //eliminate any segments that have failed
                            if (segmentTasks.Any(t => t.IsCompletedSuccessfully))
                                segmentTasks = segmentTasks.Where(t => t.IsCompletedSuccessfully).ToList();
                            else
                                throw;
                        }

                        string semAnswer = null;

                        if (segmentTasks.Count > 1 || taskIntroStage != null)
                        {
                            await CXTrace.Current.SpanFor("seg-merge").ExecuteAsync(async span =>
                            {
                                JsonNode semAnswerNode = null;

                                if (taskIntroStage != null)
                                    semAnswerNode = await taskIntroStage;

                                foreach (var segTask in segmentTasks)
                                {
                                    var seg = segTask.Result;
                                    var segNode = JsonNode.Parse(seg!);

                                    if (semAnswerNode == null)
                                    {
                                        semAnswerNode = segNode;
                                        continue;
                                    }

                                    var ctx = new JsonObject();
                                    ctx.AddCommonJsonEFunctions();
                                    ctx["seg"] = segNode;
                                    ctx["answer"] = semAnswerNode;
                                    semAnswerNode = JsonE.Evaluate(_options.SemanticSegmentMergeJsonETemplate, ctx);
                                }

                                semAnswer = (semAnswerNode ?? new JsonObject()).ToJsonString();
                                span.Output = semAnswer?.Left(5_000) + "..";
                                return Task.CompletedTask;
                            });
                        }
                        else
                        {
                            var seg = segmentTasks[0].Result;
                            semAnswer = seg;
                        }
                        
                        
                        formatContext.Answer = semAnswer ?? "";
                        answer.Answer = semAnswer;
                        formatContext.Answer = semAnswer ?? "";
                        answer.IsStructured = isStructured;

                        try
                        {
                            span.Output = new
                            {
                                Answer = isStructured
                                    ? (object)JsonSerializer.Deserialize<JsonDocument>(semAnswer!)!
                                    : semAnswer,
                                IsStructured = isStructured
                            };
                        }
                        catch (Exception ex)
                        {
                            throw new InvalidOperationException("Failure parsing structured JSON answer: " + semAnswer, ex);
                        }
                    });

                    #endregion

                    #region Suitability filter stage

                    if (!string.IsNullOrWhiteSpace(opts.SuitablePath))
                    {
                        await CXTrace.Current.SpanFor("filter-suitable", new { }).ExecuteAsync(span =>
                        {
                            var node = JsonNode.Parse(answer.Answer ?? "{}")!;
                            var path = JsonPath.Parse(opts.SuitablePath!);
                            var res = path.Evaluate(node);

                            List<JsonNode> toRemove = new();

                            foreach (var match in res.Matches)
                            {
                                if (match.Value!.GetValue<bool>())
                                    continue;

                                toRemove.Add(match.Value!);
                            }

                            foreach (var tr in toRemove)
                            {
                                //Remove the hallucination from the node node
                                var obj = tr;
                                var parent = obj.Parent;

                                while (true)
                                {
                                    //if this is a scenario with ids in an array
                                    if (parent is JsonArray arr)
                                    {
                                        arr.Remove(obj);
                                        break;
                                    }

                                    if (parent is JsonObject o)
                                    {
                                        obj = o;
                                        parent = obj.Parent;
                                    }
                                }
                            }

                            answer.Answer = node.ToJsonString();
                            answer.IsStructured = true;

                            span.Output = new
                            {
                                Answer = answer.Answer,
                                IsStructured = true,
                                Removed = toRemove.Count,
                                Total = res.Matches.Count
                            };

                            return Task.CompletedTask;
                        });
                    }

                    #endregion

                    #region Parallel stage final waiting point (row-embeddings and db filter)

                    if (!opts.EmbedBeforeSemanticFilter)
                    {
                        rowsJson = await taskRowEmbeddingStage;
                        fullRowsJson = rowsJson;
                    }

                    if (!opts.DbFilterBeforeSemanticFilter && opts.UseDbFilter)
                    {
                        var stageRes = await dbFilterStage;
                        rowsJson = stageRes.RowsJson;

                        filterRes = stageRes.FilterRes;

                        formatContext.fieldRules = stageRes.FieldRules;
                        formatContext.filterJson = stageRes.FilterJson;

                        if (stageRes.IsRefusal)
                        {
                            section.Output = new { IsRefusal = true, Answer = opts.RefusalMessage };
                            return astCtx.Record(question, new() { Answer = opts.RefusalMessage });
                        }

                        if (opts.UseEmbeddings)
                        {
                            //Incorporate embeddings too, if used.  This requires a rerun from our caches.
                            taskRowEmbeddingStage = RowEmbeddingStageAsync(opts, client, askObserve.Stopwatch, rowsJson, taskQuestionEmbeddings);
                            rowsJson = await taskRowEmbeddingStage;
                        }
                    }

                    #endregion

                    #region Anti-hallucination by key stage

                    if (opts.AntiHallucinateByKey)
                    {
                        await CXTrace.Current.SpanFor("anti-hallucinate-by-key", new { }).ExecuteAsync(async span =>
                        {
                            var node = JsonNode.Parse(answer.Answer ?? "{}")!;
                            var path = JsonPath.Parse(opts.KeyPath!);
                            var res = path.Evaluate(node);

                            Metrics.AntiHallucinate_Invocations.Inc();
                            Metrics.AntiHallucinate_KeysScanned.Inc(res.Matches.Count);

                            if(opts.UseDbFilterForAntiHullicinations && !opts.UseDbFilter)
                            {
                                var dbFilterResult = await dbFilterStage;
                                rowsJson = dbFilterResult.RowsJson;
                            }

                            List<JsonNode> toRemove = [];
                            
                            foreach (var match in res.Matches)
                            {
                                var matched = false;
                                if (rowsJson != null)
                                    foreach (var row in rowsJson)
                                    {
                                        if (row![opts.KeyField!].IsEquivalentTo(match.Value!))
                                        {
                                            matched = true;
                                            break;
                                        }
                                    }

                                //Not a hallucination
                                if (matched)
                                    continue;

                                toRemove.Add(match.Value!);
                            }

                            Metrics.AntiHallucinate_KeysRemoved.Inc(toRemove.Count);

                            toRemove.RemoveNodesFromNearestArrays();

                            answer.Answer = node.ToJsonString();
                            answer.IsStructured = true;

                            span.Output = new
                            {
                                Answer = answer.Answer,
                                IsStructured = true,
                                Removed = toRemove,
                                Total = res.Matches.Count
                            };

                            return Task.CompletedTask;
                        });
                    }

                    #endregion

                    #region Dedup by key stage

                    if (opts.DedupByKey)
                    {
                        await CXTrace.Current.SpanFor("dedup-by-key", new { }).ExecuteAsync(span =>
                        {
                            var set = new JsonNodeHashSet();
                            var node = JsonNode.Parse(answer.Answer ?? "{}")!;
                            var path = JsonPath.Parse(opts.KeyPath!);
                            var res = path.Evaluate(node);

                            var toRemove = new List<JsonNode>();

                            Metrics.DedupByKey_Invocations.Inc();
                            Metrics.DedupByKey_KeysScanned.Inc(res.Matches.Count);
                            foreach (var match in res.Matches)
                            {
                                if (set.Add(match.Value))
                                    continue;

                                toRemove.Add(match.Value!);
                            }
                            

                            Metrics.DedupByKey_DupsRemoved.Inc(toRemove.Count);
                            toRemove.RemoveNodesFromNearestArrays();        

                            answer.Answer = node.ToJsonString();
                            answer.IsStructured = true;

                            span.Output = new
                            {
                                Answer = answer.Answer,
                                IsStructured = true,
                                Removed = toRemove.Count,
                                Total = res.Matches.Count
                            };

                    

                            return Task.CompletedTask;
                        });
                    }

                    #endregion
                    
                    #region Sort Stage
                    
                    if(opts.UseSort)
                    {
                        await CXTrace.Current.SpanFor("sort-answers", new
                        {
                            Answer = answer.Answer,
                            IsStructured = answer.IsStructured,
                            SemanticFilterOutToAnswerPrompt = opts.SemanticFilterOutToAnswerPrompt,
                            ResponseFormat = responseFormatBase?.ToString()
                        }).ExecuteAsync(async span =>
                        {
                            answer.Answer = SortJson(answer.Answer, (await taskSortStage).Condition, fullRowsJson);
                            if (opts.UseDbFilter && rowsJson != null)
                                rowsJson = OrderJson(JsonNodeComparer.Instance, (row, field) => row[field], rowsJson.Select(x => new SortJsonClass(x)).ToList(), (await taskSortStage).Condition).Select(x => x.JsonRow).ToList();
                            span.Output = new
                            {
                                Answer = answer.Answer
                            };
                        });
                    }

                    #endregion
                    
                    #region SemanticFilterOutToAnswer stage

                    if (!string.IsNullOrWhiteSpace(opts.SemanticFilterOutToAnswerPrompt))
                    {
                        await CXTrace.Current.SpanFor("semantic-filter-out-to-answer", new
                        {
                            Answer = answer.Answer,
                            IsStructured = answer.IsStructured,
                            SemanticFilterOutToAnswerPrompt = opts.SemanticFilterOutToAnswerPrompt,
                            ResponseFormat = responseFormatBase?.ToString()
                        }).ExecuteAsync(async span =>
                        {

                            var agent = _sp.GetRequiredNamedService<IChatAgent>(_options.SemanticFilterOutToAnswerAgentName);
                            
                            var outToAnswerReq = agent.GetRequest();
                            outToAnswerReq.ResponseFormatBase = responseFormatBase;
                            outToAnswerReq.SystemPrompt = Smart.Format(opts.SemanticFilterOutToAnswerPrompt, formatContext);
                            outToAnswerReq.History.AddRange(astCtx.History);
                            
                            var outToAnswer = await agent.RequestAsync(outToAnswerReq);
                            answer.Answer = outToAnswer.Answer;
                            formatContext.Answer = outToAnswer.Answer ?? "";
                            answer.IsStructured = outToAnswerReq.ResponseFormatBase != null;

                            span.Output = new
                            {
                                Answer = answer.Answer,
                                IsStructured = answer.IsStructured
                            };
                        });
                    }

                    #endregion
                    
                    var keyedRows = BuildKeyedRows(fullRowsJson, opts.KeyField);
                    var semanticAnswer = answer.Answer;

                    #region Soft Filter and Sort
                    if (opts.UseSoftFilter && opts.UseDbFilterForAntiHullicinations)
                        answer.Answer = AnswerSoftFilterSort(answer.Answer, (await dbFilterStage).FilterJson, keyedRows);
                    #endregion
                    
                    #region Final Fuzzy Sort
                    await CXTrace.Current.SpanFor("fuzzy-sort", new
                        {
                            BeforeSort = answer.Answer,
                        }).ExecuteAsync( _ =>
                        {
                            answer.Answer = SortJson(answer.Answer, [
                                new SortCondition()
                                {
                                    Field = opts.SimilarityField ?? FlatQueryAssistantOptions.DefaultSimilarityField,
                                    SortOrder = "DESC"
                                }
                            ], rowsJson);
                            return Task.FromResult(answer.Answer);
                        });
                    #endregion
                    
                    #region JsonE stage

                    if (answer.IsStructured && opts.JsonEOutputTemplate != null)
                    {
                        await CXTrace.Current.SpanFor("json-e", new
                        {
                            answer = answer.Answer,
                            rows = rowsJson?.Count ?? 0,
                            keyPath = opts.KeyPath,
                            keyField = opts.KeyField,
                            template = opts.JsonEOutputTemplate
                        }).ExecuteAsync(async span =>
                        {
                            using var jsonELock = await _slimLockJsonE.UseWithTraceAsync();

                            var node = JsonNode.Parse(answer.Answer ?? "{}")!;
                            
                            if (!string.IsNullOrWhiteSpace(opts.KeyPath) && !string.IsNullOrWhiteSpace(opts.KeyField) && opts.JsonEIncludeRestRows)
                            {
                                var path = JsonPath.Parse(opts.KeyPath);
                                var res = path.Evaluate(node);
                                var usedKeys = new List<JsonNode>();
                                foreach (var match in res.Matches)
                                    usedKeys.Add(match.Value!);

                                var restRows = new JsonArray();
                                if (rowsJson != null)
                                    for (var i = 0; i < rowsJson.Count; i++)
                                    {
                                        var row = rowsJson[i];
                                        var key = row![opts.KeyField];
                                        var used = false;
                                        foreach (var uk in usedKeys)
                                        {
                                            if (key.IsEquivalentTo(uk))
                                            {
                                                used = true;
                                                break;
                                            }
                                        }

                                        if (!used)
                                            restRows.Add(row!.DeepClone());
                                    }

                                node["restRows"] = restRows;
                            }

                            if (filterRes != null)
                                node["filters"] = JsonNode.Parse(filterRes.Answer!);
                            else
                                node["filters"] = new JsonObject();
                            if(opts.UseSort)
                            {
                                var sortStage = await taskSortStage;
                                if (sortStage != null)
                                    node["sort"] = JsonNode.Parse(sortStage.SortRes.Answer!);
                                else 
                                    node["sort"] = new JsonObject();
                            }

                            if (rowsJson != null)
                                // ReSharper disable once CoVariantArrayConversion
                                node["rows"] = new JsonArray(rowsJson.ToArray());
                            else
                                node["rows"] = new JsonArray();

                            if (opts.JsonEIncludeFullRows)
                                if (fullRowsJson != null)
                                    // ReSharper disable once CoVariantArrayConversion
                                    node["fullRows"] = new JsonArray(fullRowsJson.Select(r => r.DeepClone()).ToArray());
                                else
                                    node["fullRows"] = new JsonArray();

                            node["get_full_row"] = JsonFunction.Create((arguments, _) =>
                                keyedRows.GetValueOrDefault(arguments[0] ?? new JsonObject()));
                            answer.Answer = JsonE.Evaluate(opts.JsonEOutputTemplate, node.AddCommonJsonEFunctions())?.ToString();
                            answer.IsStructured = true;
                            span.Output = new
                            {
                                AnswerLength = answer.Answer?.Length ?? 0,
                                IsStructured = true
                            };
                        });
                    }
                    #endregion

                    if (!astCtx.IsKeepAlive && opts.EnableAnswerLogging)
                    {
                        var answerLogClient =
                            _sp.GetRequiredNamedService<PostgreSQLClient>(opts.AnswerLogPostgresSQLClientName);
                        MiscHelpers.FireAndForget(() => answerLogClient.ExecuteAsync($"CREATE TABLE IF NOT EXISTS public.answers_log (question varchar NULL,answer jsonb NULL,assistant varchar NULL,id serial4 NOT NULL,question_time timestamp DEFAULT CURRENT_TIMESTAMP NULL,CONSTRAINT qanda_logs_pk PRIMARY KEY (id))"), _logger);
                        MiscHelpers.FireAndForget(() => answerLogClient.ExecuteAsync($"DELETE FROM answers_log WHERE question_time < CURRENT_TIMESTAMP - INTERVAL '{new InjectRaw($"{(opts.AnswerLogExpiryTimeSpan?.TotalHours ?? opts.AnswerLogExpiryHours):###0}")} hours'"),  _logger);
                        
                        MiscHelpers.FireAndForget(async () =>
                        {
                            var path = JsonPath.Parse(opts.KeyPath);
                            var matches = path.Evaluate(JsonNode.Parse(semanticAnswer ?? "{}"));
                            var rowSemanticAnswer = matches.Matches.Select(x => keyedRows.GetValueOrDefault(x.Value));
                            var jsonAnswer = new JsonObject();
                            jsonAnswer["JsonEAnswer"] = JsonSerializer.Deserialize<JsonObject>(answer.Answer);
                            jsonAnswer["FullRows"] = new JsonArray(fullRowsJson.Select(x => x.DeepClone()).ToArray());
                            jsonAnswer["SemanticAnswer"] = new JsonArray(rowSemanticAnswer.Select(x => x.DeepClone()).ToArray());
                            var cmd = new NpgsqlCommand();
                            cmd.CommandText =
                                "INSERT INTO answers_log (question, answer, assistant) VALUES (@question, @answer::jsonb, @assistant)";
                            cmd.Parameters.AddWithValue("question", question);
                            cmd.Parameters.AddWithValue("answer", jsonAnswer.ToJsonString());
                            cmd.Parameters.AddWithValue("assistant", _name);
                            await answerLogClient.ExecuteAsync(cmd);
                        }, _logger);
                    }

                    #region SmartFormat stage

                    if (responseFormatBase == null && !string.IsNullOrWhiteSpace(opts.AnswerSmartFormat) && answer.IsStructured)
                    {
                        await CXTrace.Current.SpanFor("smart-format", new
                        {
                            AnswerLength = answer.Answer?.Length,
                            IsStructured = true
                        }).ExecuteAsync(span =>
                        {
                            answer.Answer ??= "";
                            JsonDocument doc = JsonDocument.Parse(answer.Answer);
                            formatContext.Answer = doc.ToDynamic()!;
                            answer.Answer = Smart.Format(opts.AnswerSmartFormat, formatContext);

                            if (answer.Answer.Length > opts.AnswerSmartFormatCharLimit)
                                answer.Answer = answer.Answer.Left(opts.AnswerSmartFormatCharLimit) + "...";

                            answer.IsStructured = false;
                            span.Output = new
                            {
                                Answer = answer.Answer,
                                IsStructured = false
                            };
                            return Task.CompletedTask;
                        });
                    }

                    #endregion

                    #region Strip Regex stage

                    if (opts.StripRegex != null && answer.Answer != null)
                    {
                        await CXTrace.Current.SpanFor("strip-regex", new
                        {
                            AnswerLength = answer.Answer?.Length,
                            IsStructured = answer.IsStructured
                        }).ExecuteAsync(span =>
                        {
                            answer.Answer = string.Join("", Regex.Matches(answer.Answer, opts.StripRegex).Select(m => m.Value));
                            answer.IsStructured = false;
                            span.Output = new
                            {
                                Answer = answer.Answer,
                                IsStructured = false
                            };
                            return Task.CompletedTask;
                        });
                    }

                    #endregion

                    if (astCtx.EligibleForContextAi && !string.IsNullOrWhiteSpace(answer.Answer))
                        _contextAiService.EnqueueAndForget(new LogThreadMessageRequest(astCtx.SessionId!, "system", astCtx.UserId!,
                            answer.Answer.Preview(10_000)));

                    if (opts.SlowThreshold.HasValue && askObserve.Stopwatch.Elapsed >= opts.SlowThreshold.Value)
                        Metrics.AsksSlow.Inc();
                    if(_options.UseChatCache && !astCtx.IsKeepAlive)
                        _chatCache.GetOrAdd(question, _ => answer);
                    
                    return astCtx.Record(question, answer);
                });
        }
        catch (Exception ex) when (ex is TimeoutException or FlurlHttpTimeoutException or TaskCanceledException)
        {
            Metrics.AsksNotCompletedByException.Inc();
            Metrics.AsksNotCompletedByTimeoutException.Inc();
            throw;
        }
        catch
        {
            Metrics.AsksNotCompletedByException.Inc();
            throw;
        }
    }

    public void Dispose()
    {
        Metrics?.Dispose();
        _embeddingCache?.Dispose();
        _optionsChangeDisposable?.Dispose();
        _ctsDisposed.Cancel();
    }
}
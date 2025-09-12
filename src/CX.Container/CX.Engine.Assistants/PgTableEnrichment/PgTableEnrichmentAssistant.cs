using System.Dynamic;
using System.Text;
using System.Text.Json.Nodes;
using CX.Engine.Assistants.Channels;
using CX.Engine.ChatAgents;
using CX.Engine.ChatAgents.OpenAI;
using CX.Engine.Common;
using CX.Engine.Common.Formatting;
using CX.Engine.Common.IronPython;
using CX.Engine.Common.JsonSchemas;
using CX.Engine.Common.PostgreSQL;
using CX.Engine.Common.Tracing;
using CX.Engine.Common.Tracing.Langfuse;
using Cx.Engine.Common.PromptBuilders;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmartFormat;

namespace CX.Engine.Assistants.PgTableEnrichment;

public class PgTableEnrichmentAssistant : IAssistant, IDisposable
{
    private readonly string _name;
    private readonly IDisposable _optionsMonitorDisposable;
    private readonly IServiceProvider _sp;
    private readonly LangfuseService _langfuseService;
    private readonly ILogger _logger;
    private Snapshot _snapshot;

    private class Snapshot
    {
        public PgTableEnrichmentAssistantOptions Options;
        public IChatAgent ChatAgent;
    }

    private void SetSnapshot(PgTableEnrichmentAssistantOptions options)
    {
        var ss = new Snapshot();
        ss.Options = options;
        ss.ChatAgent = _sp.GetRequiredNamedService<IChatAgent>(options.ChatAgentName);
        _snapshot = ss;
    }

    public PgTableEnrichmentAssistant(string name, IOptionsMonitor<PgTableEnrichmentAssistantOptions> monitor, ILogger logger, IServiceProvider sp,
        [NotNull] LangfuseService langfuseService)
    {
        _name = name ?? throw new ArgumentNullException(nameof(name));
        _sp = sp ?? throw new ArgumentNullException(nameof(sp));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _langfuseService = langfuseService ?? throw new ArgumentNullException(nameof(langfuseService));
        _optionsMonitorDisposable = monitor.Snapshot(() => _snapshot?.Options, SetSnapshot, logger, sp);
    }

    public Task<AssistantAnswer> AskAsync(string question, AgentRequest astCtx) =>
        CXTrace.TraceOrSpan(() => new CXTrace(_langfuseService, astCtx.UserId, astCtx.SessionId).WithName(question).WithTags("pg-table-enrichment").WithTags(_name),
            trace => trace.SpanFor("pg-table-enrichment", new { Question = question })).ExecuteAsync<AssistantAnswer>(async section =>
        {
            var ss = _snapshot;
            var opts = ss.Options;

            List<PgTableEnrichmentOperation> ops = [];
            var userAnswer = "";

            {
                var agent = ss.ChatAgent;
                var req = agent.GetRequest(question);
                var pb = new PromptBuilder();
                var sb = new StringBuilder();
                foreach (var op in opts.Operations)
                    sb.AppendLine($"- {op.Id}: {op.Description}");
                pb.Context.OperationList = sb.ToString();
                pb.Add(opts.Prompt);
                var prompt = pb.GetPrompt();
                req.SystemPrompt = prompt;
                var schema = agent.GetSchema("pg_table_enrich_" + _name);
                schema.Object.AddProperty("Answer", PrimitiveTypes.String);
                schema.Object.AddProperty("Reasoning", PrimitiveTypes.String);
                schema.Object.AddProperty("Operations", PrimitiveTypes.Array, itemType: PrimitiveTypes.String,
                    choices: opts.Operations.Select(op => op.Id).ToList());
                req.SetResponseSchema(schema);
                var res = await agent.RequestAsync(req);
                var resJson = JsonNode.Parse(res.Answer);
                userAnswer = resJson!["Answer"]!.AsValue().GetValue<string>();
                foreach (var opNode in resJson["Operations"]!.AsArray())
                {
                    var opId = opNode.AsValue().GetValue<string>();
                    var op = opts.Operations.First(o => o.Id == opId);
                    ops.Add(op);
                }
            }

            foreach (var op in ops)
            {
                await CXTrace.Current.SpanFor(op.Id, new { }).ExecuteAsync(async _ =>
                {
                    _logger.Log(op.OpInfoLogLevel, op.StartMessage);
                    try
                    {
                        var channel = _sp.GetRequiredNamedService<Channel>(op.ChannelName);
                        var assistant = channel.Assistant;
                        var sql = _sp.GetRequiredNamedService<PostgreSQLClient>(op.PostgreSQLClientName);

                        var limit = "";

                        if (op.RowLimit > 0)
                            limit = $"LIMIT {op.RowLimit}";
                        
                        dynamic context = new ExpandoObject();
                        context.sql = sql;

                        var pyContext = new IronPythonContext();
                        pyContext.Variables["context"] = context;
                        
                        if (!string.IsNullOrWhiteSpace(op.Init))
                            await IronPythonExecutor.ExecuteScriptAsync(op.Init, pyContext);

                        var responseSchema = await IronPythonHelper.ResolveJsonNodeAsync(op.ResponseSchema.DeepClone(), pyContext);
                        
                        List<Dictionary<string, object>> rows;
                        {
                            dynamic sqlContext = new ExpandoObject();
                            sqlContext.TableName = op.TableName;
                            sqlContext.LimitClause = limit;

                            var retrieveSql = """
                                              SELECT * 
                                              FROM {$TableName} 
                                              {$LimitClause}
                                              """;

                            if (!string.IsNullOrWhiteSpace(op.RetrieveSql))
                                retrieveSql = op.RetrieveSql;

                            var cmd = await NpgsqlCommandStringFormatter.FormatAsync(retrieveSql, sqlContext);

                            rows = await sql.ListDictionaryAsync(cmd);
                        }

                        var rowSemaphore = new SemaphoreSlim(op.MaxParallelRows, op.MaxParallelRows);

                        var tasks = new List<Task>();
                        foreach (var row in rows)
                        {
                            Task ProcessRowAsync(Dictionary<string, object> row)
                            {
                                var rowId = CxSmart.Format(op.RowIdentifier, row);
                                return CXTrace.Current.SpanFor(rowId, new { }).ExecuteAsync(async span =>
                                {
                                    try
                                    {
                                        dynamic promptContext = new ExpandoObject();
                                        promptContext.question = question;
                                        promptContext.row = row;
                                        promptContext.context = context;

                                        var pb = new PromptBuilder();
                                        pb.Add(new PromptContentSection(CxSmart.Format(op.Prompt, promptContext), applySmartFormat: false));
                                        var req = new AgentRequest();
                                        req.UserId = astCtx.UserId;
                                        req.SessionId = astCtx.SessionId;
                                        if (responseSchema != null)
                                            req.Overrides.Add(new ResponseFormatOverride()
                                            {
                                                ResponseFormat = new OpenAISchemaResponseFormat(responseSchema)
                                            });
                                        var prompt = pb.GetPrompt();

                                        if (op.PromptPreviewLimit > 0)
                                            prompt = prompt.Preview(op.PromptPreviewLimit);

                                        if (op.QuestionLogLevel != LogLevel.None && _logger.IsEnabled(op.QuestionLogLevel))
                                            _logger.Log(op.QuestionLogLevel, "> " + prompt);
                                        var res = await assistant.AskAsync(prompt, req);
                                        if (op.AnswerLogLevel != LogLevel.None && _logger.IsEnabled(op.AnswerLogLevel))
                                            _logger.Log(op.AnswerLogLevel, "< " + res.Answer);

                                        var answer = res.Answer.IfJsonToPrimitive();
                                        span.Output = new
                                        {
                                            Answer = answer
                                        };
                                        using var con = await sql.GetOpenConnectionAsync();
                                        using var cmd = await NpgsqlCommandStringFormatter.FormatAsync(op.UpdateSql, new
                                        {
                                            answer = answer,
                                            row = row
                                        });
                                        cmd.Connection = con;
                                        if (op.UpdateScript != null)
                                            await IronPythonExecutor.ExecuteScriptAsync(op.UpdateScript, new
                                            {
                                                cmd = cmd,
                                                answer = answer
                                            });
                                        await cmd.ExecuteNonQueryAsync();
                                    }
                                    finally
                                    {
                                        rowSemaphore.Release();
                                    }
                                });
                            }

                            await rowSemaphore.WaitAsync();
                            tasks.Add(ProcessRowAsync(row));
                        }

                        await Task.WhenAll(tasks);
                    }
                    finally
                    {
                        _logger.Log(op.OpInfoLogLevel, op.FinishMessage);
                    }
                });
            }

            section.Output = new
            {
                Answer = userAnswer
            };
            return new(userAnswer);
        });

    public void Dispose()
    {
        _optionsMonitorDisposable?.Dispose();
    }
}
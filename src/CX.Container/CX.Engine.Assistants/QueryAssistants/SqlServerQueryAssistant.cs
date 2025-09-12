using System.Text;
using CX.Engine.Assistants.ArtifactAssists;
using CX.Engine.ChatAgents;
using CX.Engine.ChatAgents.OpenAI;
using CX.Engine.Common;
using CX.Engine.Common.JsonSchemas;
using CX.Engine.Common.SqlServer;
using CX.Engine.Common.Stores.Json;
using CX.Engine.Common.Tracing;
using CX.Engine.Common.Tracing.Langfuse;
using JetBrains.Annotations;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CX.Engine.Assistants.QueryAssistants;

public class SqlServerQueryAssistant : IAssistant, IDisposable
{
    private readonly IDisposable _optionsMonitorDisposable;
    private readonly string _name;
    private readonly ILogger _logger;
    private readonly IServiceProvider _sp;
    private readonly LangfuseService _langfuseService;
    private readonly Crc32JsonStore _crc32JsonStore;
    private readonly ArtifactAssist _artifactAssist;
    private readonly AsyncLocal<OpContextClass> OpContext = new();

    public string FullName => "sql-server-query." + _name;
    private Snapshot _snapshot;
    private ValueTask<SemaphoreSlimDisposable> UseFeedbackSlimLockAsync => (OpContext.Value?.FeedbackSlimlock?.UseAsync()).IfNull(new());
    private Snapshot OpContextSnapshot => OpContext.Value?.Snapshot ?? _snapshot;
    
    private class Snapshot
    {
        public SqlServerQueryAssistantOptions Options;
        public IChatAgent ChatAgent;
        public SqlServerClient Sql; 
    }
    
    private class OpContextClass
    {
        public SemaphoreSlim FeedbackSlimlock;
        public Snapshot Snapshot;
    }

    private void SetSnapshot(SqlServerQueryAssistantOptions options)
    {
        var ss = new Snapshot();
        ss.Options = options;
        ss.ChatAgent = (OpenAIChatAgent)_sp.GetRequiredNamedService<IChatAgent>(options.ChatAgentName);
        
        if (ss.Options.CanExecuteQueries)
            ss.Sql = (SqlServerClient)_sp.GetRequiredNamedService<SqlServerClient>(options.SQLServerClientName);

        if (options.CacheQuestions)
            ss.ChatAgent = new CachedChatAgent(_crc32JsonStore, (options.CachePostgreSQLClientName, options.CacheTableName), (OpenAIChatAgent)ss.ChatAgent);
        
        _snapshot = ss;
    }

    public SqlServerQueryAssistant([NotNull] string name, IOptionsMonitor<SqlServerQueryAssistantOptions> optionsMonitor, [NotNull] ILogger logger, IServiceProvider sp, [NotNull] LangfuseService langfuseService,
        [NotNull] Crc32JsonStore crc32JsonStore, [NotNull] ArtifactAssist artifactAssist)
    {
        _name = name ?? throw new ArgumentNullException(nameof(name));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _sp = sp ?? throw new ArgumentNullException(nameof(sp));
        _langfuseService = langfuseService ?? throw new ArgumentNullException(nameof(langfuseService));
        _crc32JsonStore = crc32JsonStore ?? throw new ArgumentNullException(nameof(crc32JsonStore));
        _artifactAssist = artifactAssist ?? throw new ArgumentNullException(nameof(artifactAssist));
        _optionsMonitorDisposable = optionsMonitor.Snapshot(() => _snapshot?.Options, SetSnapshot, logger, sp);
    }

    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    private class AskResponse
    {
        public string ResponseMessage { get; set; }
        public string SqlToExecute { get; set; }
    }
    
    private async void LogFeedback(LogLevel level, string feedback)
    {
        using var _ = await UseFeedbackSlimLockAsync;
        _logger.Log(level, feedback);
    }


    [SemanticAction]
    [UsedImplicitly]
    [SemanticNote("Executes the SQL statement against the SQL Server database and returns the result.")]
    private async Task<string> ExecuteSql(string sql)
    {
        var ss = OpContextSnapshot;
        
        if (ss.Options.SelectOnly && !SqlValidator.IsSelectOnly(sql))
            throw new ArtifactException("Only SELECT statements are allowed.");

        using var con = await ss.Sql.GetOpenConnectionAsync();
        using var cmd = new SqlCommand();
        cmd.CommandText = sql;
        cmd.Connection = con;
        var rdr = await cmd.ExecuteReaderAsync();
        var res = await SqlMarkdownConverter.ConvertToMarkdownAsync(rdr, ss.Options.MaxRows);
        
        if (ss.Options.DiscordMode)
            res = "```\r\n" + res + "\r\n```";
        
        return res;
    }

    public async Task<AssistantAnswer> AskAsync(string question, AgentRequest astCtx)
    {
        AssistantsSharedAsyncLocal.EnterAsk();

        //File.ReadAllText("d:\\cx\\clients\\gic\\dbo schema.sql");
        //convert string to JSON and save in schema.json
        //File.WriteAllText("d:\\cx\\clients\\gic\\dbo schema.json", JsonSerializer.Serialize(File.ReadAllText("d:\\cx\\clients\\gic\\schema.sql")));
        // Environment.Exit(-1);
        
        var ss = _snapshot;
        var opts = ss.Options;
        OpContext.Value = new();
        OpContext.Value.Snapshot = ss;
        OpContext.Value.FeedbackSlimlock = astCtx.FeedbackLock;

        var section = CXTrace.TraceOrSpan(
            () => new CXTrace(_langfuseService, astCtx.UserId, astCtx.SessionId).WithTags("sql-server-query", _name).WithName(question),
            trace => trace.SpanFor(FullName, new { Question = question }));
        
        return await section.ExecuteAsync(async _ =>
        {
            var aaReq = new ArtifactAssistRequest()
            {
                Agent = ss.ChatAgent,
                Question = question,
                
            };
            aaReq.History.AddRange(astCtx.History);
            
            var sb = new StringBuilder();
            var req = new ArtifactAssistRequest
            {
                Question = question,
                Agent = ss.ChatAgent,
                SchemaName = "sqlserverquery",
                UseExecutionPlan = ss.Options.UseExecutionPlan,
                ReasoningEffort = ss.Options.ReasoningEffort,
                DebugMode = ss.Options.DebugMode,
                AddChangeArtifactAction = false,
                AddNoAction = true,
                OnStartAction = (action, args) => LogFeedback(opts.MessageLogLevel, "> " + action.GetCallSignature(args)),
                OnAddedToHistory = msg =>
                {
                    if (msg.Role != "user")
                    {
                        if (opts.KeepLastMessageOnly)
                        {
                            sb.Clear();
                            sb.Append(msg.Content);
                        }
                        else
                        {

                            if (sb.Length > 0)
                                sb.AppendLine("========================================");

                            sb.AppendLine(msg.Content);
                        }

                        LogFeedback(opts.MessageLogLevel, "========================================\r\n" + msg.Content);
                    }

                    astCtx.History.Add(msg);
                }
            };
            req.Prompt.Context.SchemaDefinition = ss.Options.SchemaDefinition;
            req.Prompt.Instructions.Content = ss.Options.SystemPrompt;
            
            if (ss.Options.CanExecuteQueries)
                req.Actions.AddFromObject(this, req.DebugMode);
            
            req.History.AddRange(astCtx.History);

            await _artifactAssist.RequestAsync(req);

            section.Output = new
            {
                Prompt = req.Prompt.GetPrompt().Preview(opts.LangfuseMaxStringLen),
                Answer = sb.ToString().Preview(opts.LangfuseMaxStringLen)
            };
            
            return new AssistantAnswer(sb.ToString());
        });
    }

    public void Dispose()
    {
        _optionsMonitorDisposable?.Dispose();
    }
}
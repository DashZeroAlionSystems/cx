using System.Dynamic;
using System.Text;
using CX.Engine.Assistants.ArtifactAssists;
using CX.Engine.ChatAgents;
using CX.Engine.Common;
using CX.Engine.Common.Formatting;
using CX.Engine.Common.IronPython;
using CX.Engine.Common.SqlServer;
using CX.Engine.Common.Tracing;
using CX.Engine.Common.SqlKata;
using CX.Engine.Common.Tracing.Langfuse;
using Cx.Engine.Common.PromptBuilders;
using JetBrains.Annotations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SqlKata;
using SqlKata.Execution;

namespace CX.Engine.Assistants.QueryAssistants;

public class SqlServerReportAssistant : IAssistant, ISnapshottedOptions<SqlServerReportAssistant.Snapshot, SqlServerReportAssistantOptions, SqlServerReportAssistant>
{
    public readonly string Name;
    public readonly string FullName;
    public readonly string SchemaName;
    private readonly ILogger _logger;
    private readonly IServiceProvider _sp;
    private readonly LangfuseService _langfuseService;
    private readonly ArtifactAssist _artifactAssist;
    public readonly DynamicSlimLock SlimLock = new(3);

    public class Snapshot : Snapshot<SqlServerReportAssistantOptions, SqlServerReportAssistant>, ISnapshotSyncInit<SqlServerReportAssistantOptions>
    {
        public IChatAgent Agent;
        public SqlServerClient Sql;
        public void Init(IConfigurationSection section, ILogger logger, IServiceProvider sp)
        {
            sp.GetRequiredNamedService(out Agent, Options.ChatAgentName, section);
            sp.GetRequiredNamedService(out Sql, Options.SqlServerClientName, section);
            Instance.SlimLock.SetMaxCount(Options.MaxConcurrentQuery);
        }
    }

    public SqlServerReportAssistant([NotNull] string name, MonitoredOptionsSection<SqlServerReportAssistantOptions> optionsSection, [NotNull] ILogger logger,
        [NotNull] IServiceProvider sp,
        [NotNull] LangfuseService langfuseService,
        [NotNull] ArtifactAssist artifactAssist)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _sp = sp ?? throw new ArgumentNullException(nameof(sp));
        _langfuseService = langfuseService ?? throw new ArgumentNullException(nameof(langfuseService));
        _artifactAssist = artifactAssist ?? throw new ArgumentNullException(nameof(artifactAssist));
        FullName = $"{AssistantDI.SqlServerReportEngineName}.{Name}";
        SchemaName = $"{AssistantDI.SqlServerReportEngineName}_{Name}";
        
        optionsSection.Bind<Snapshot, SqlServerReportAssistant>(this);
    }

    private Query HandleRelation(QueryFactory factory, string sql, string relationName)
    {
        return sql == null ? factory.Query(relationName) : factory.Query().FromRaw($"({sql}) AS {relationName}");
    }

    public Task<AssistantAnswer> AskAsync(string question, AgentRequest astCtx)
    {
        var ss = CurrentShapshot;
        var opts = ss.Options;
        var agent = ss.Agent;
        var sql = ss.Sql;
        var aa = _artifactAssist;

        var section = CXTrace.TraceOrSpan(
            () => new CXTrace(_langfuseService, astCtx.UserId, astCtx.SessionId).WithTags(AssistantDI.SqlServerReportEngineName, Name).WithName(question),
            trace => trace.SpanFor(FullName, new { }));

        return section.ExecuteAsync(async unused =>
        {
            var t = Convert.ChangeType("9999/12/31 23:59:59", typeof(DateTime));
            using var _ = await SlimLock.UseAsync();
            
            dynamic _context = new ExpandoObject();
            await CXTrace.Current.SpanFor("Init-Scripts").ExecuteAsync(async _ =>
            {
                string optsQuery = opts.Sql;
                string relName = opts.RelationName;
                List<IDisposable> disposables = [];

                List<IronPythonScript> pythonScripts = [];
                foreach (var call in opts.Init)
                {
                    var _db = await sql.GetQueryFactory();
                    disposables.Add(_db);
                    IronPythonScript pyScript = new()
                    {
                        ScopeVariables = new Dictionary<string, object>()
                            {
                                { "context", _context },
                                { "query", HandleRelation(_db, optsQuery, relName) },
                                { "relation", relName }
                            },
                        Script = call
                    };
                    pyScript.ImportExtensions?.Invoke(["cs"]);
                    pythonScripts.Add(pyScript);
                }
                await IronPythonExecutor.ExecuteScriptsAsync(pythonScripts);
                await IronPythonHelper.ResolveObjectAsync(_context);
                disposables.DisposeAll();
             });

            ChatMessage lastMessage = null;
            string lastQueryResult = null;
            bool isDuplicateException = false;
            var aar = new ArtifactAssistRequest()
            {
                Agent = agent,
                Question = question,
                SchemaName = SchemaName,
                LangfuseService = _langfuseService,
                OnAddedToHistory = (msg) => { lastMessage = msg; },
                AddChangeArtifactAction = false,
                OneShot = opts.AllowOneShot,
                AllowDuplicateExceptions = opts.AllowDuplicateExceptions,
                AllowDuplicateExceptionsMessage = opts.AllowDuplicateExceptionsMessage,
                OnDuplicateException = (isDuplicate) => { isDuplicateException = isDuplicate; }
            };
            
            var act = new AgentAction("RunQuery");
            
            lock (astCtx.History)
            {
                var historyTokens = 0;

                for (var i = astCtx.History.Count - 1; i >= 0; i--)
                {
                    var h = astCtx.History[i];
                    var hTokens = TokenCounter.CountTokens(h.Role + ": ") + TokenCounter.CountTokens(h.Content);

                    if (historyTokens + hTokens > opts.CutoffHistoryTokens)
                        break;

                    historyTokens += hTokens;
                    aar.History.Insert(0, h);
                }
            }

            var sqlAssist = new SqlKataAssist(_context);
            act.OnJsonActionAsync = async doc =>
            {
                try
                {
                    using var _db = await sql.GetQueryFactory();
                    // Convert the JSON document to a primitive dictionary.
                    var docPrimitive = doc.RootElement.ToPrimitive(true);
                    if (!(docPrimitive is Dictionary<string, object> parameters))
                        throw new ArgumentException($"{nameof(doc)} is not a valid object");

                    // Execute the SQL script using the provided parameters.
                    var query = HandleRelation(_db, opts.Sql, opts.RelationName);
                    SqlKataAssistResult assistResult = null;
                    if(opts.Parameters.Count > 0)                   
                            assistResult = await sqlAssist.ExecuteScript(query, parameters, opts.RunQuery);                        

                    string result = "No further actions possible, Parameters has 0 items";
                    if (opts.RunQuery && assistResult != null)
                    {
                        // Convert the query results to Markdown if the query was executed.
                        var markdown = await SqlMarkdownConverter.ConvertToMarkdownAsync(assistResult.Results as List<dynamic>, opts.MaxRows, "<No rows returned>", assistResult.Formats);
                        var sb = new StringBuilder();
                        if(opts.ShowRawSql)
                        {
                            sb.AppendLine("Raw Sql result:");
                            sb.AppendLine(assistResult.Sql);
                            sb.AppendLine();
                        }

                        if (opts.ShowSelections)
                        {
                            sb.AppendLine("Selections:");
                            sb.AppendLine(assistResult.Selections.ToSelectionTree(indentSize: 3));
                            sb.AppendLine();
                        }

                        // If in Discord mode, wrap the result in a code block.
                        if (opts.DiscordMode)
                        {
                            if (markdown != null)
                                markdown = "```\r\n" + markdown + "\r\n```";
                        }
                        
                        if(opts.ShowRawSql)
                            sb.AppendLine("Query result:");
                        sb.AppendLine(markdown);
                        lastQueryResult = sb.ToString();
                        result = sb.ToString();
                    }
                    else if(assistResult != null)
                    {
                        // Otherwise, return the compiled SQL string.
                        lastQueryResult = assistResult.Sql;
                        result = assistResult.Sql;
                    }
                    
                    if(opts.MaxCharactersPerCall != 0)
                        result = result.Preview(opts.MaxCharactersPerCall);

                    return result;
                }
                catch (Exception ex)
                {
                    // Propagate inner exception if available, otherwise rethrow the caught exception.
                    throw ex.InnerException ?? ex;
                }
            };
            aar.Actions.Add(act);
            if (ss.Options.Parameters.Count > 0)
            {
                var pb = new PromptBuilder();
                var intro = new PromptContentSection() { Order = 0, Content = ss.Options.SystemPrompt };
                var sds = new StructureDescriptionSection() { Order = 1 };
                    
                pb.Add(intro);

                foreach (var par in ss.Options.Parameters)
                {
                    var context = new IronPythonContext();
                    context.Variables.Add("context", _context);
                    sds.Fields.Add(par.Name, new(await CxSmart.LazyFormatAsync(par.LlmDescription, (_context as IDictionary<string, object>).ToStubbedLazyDictionary()), new() { ["DefaultValue"] = par.DefaultValue }));
                    List<string> choices = null;
                    if (par.Choices != null)
                    {
                        choices = [];
                        choices.AddRange(par.Choices);
                        await IronPythonHelper.ResolveArrayAsync(choices, context);
                    }
                    sqlAssist.AddProperty(new()
                    {
                        Functions = par.Functions,
                        Name = par.Name,
                        Type = par.ParsedType.ToSchemaType(),
                        Description = par.LlmDescription,
                        IsDate = par.ParsedType == SqlServerReportType.DateTime || par.ParsedType == SqlServerReportType.Date,
                        Choices = choices,
                        DefaultValue = par.DefaultValue,
                        Format = par.FormatString,
                        AllowMultiple = par.AllowMultiple
                    });
                }

                pb.Add($"Todays date is: {DateTime.Now.ToShortDateString()}");
                
                if (opts.CompileSchema)
                    pb.Add(sqlAssist.GetPromptBuilder().GetPrompt());
                else
                    pb.Add(sds);
                
                var schema = sqlAssist.GetSchemaObject(opts.AllowLimits);
                if(opts.CompileKeys != null)
                    foreach(var prop in opts.CompileKeys)
                        sds.Fields.Add(prop, new(sqlAssist.CompileProperty(prop, order: (sds.Order ?? 1) + 1)));
                
                act.UsageNotes = pb.GetPrompt();
                act.Object.Concat(schema);
            }

            await aa.RequestAsync(aar);

            var response = new StringBuilder();
            response.AppendLine();
            if (lastQueryResult != null)
                response.AppendLine(lastQueryResult);
            else
            {
                if (ss.Options.CustomNoQueryResponseMessageEnabled && !isDuplicateException)
                    response.AppendLine(ss.Options.CustomNoQueryResponseMessage);
                else if (!ss.Options.AllowDuplicateExceptions && isDuplicateException)
                    response.AppendLine(ss.Options.AllowDuplicateExceptionsMessage);
                else
                    response.AppendLine(lastMessage?.Content ?? "No query response generated.");
            }

            return new AssistantAnswer(response.ToString());
        });
    }

    public Snapshot CurrentShapshot { get; set; }
    public MonitoredOptionsSection<SqlServerReportAssistantOptions> OptionsSection { get; set; }

    public void Dispose()
    {
    }
}
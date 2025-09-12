using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using CX.Engine.Assistants.AssessmentBuilder.Xml;
using CX.Engine.Archives;
using CX.Engine.Archives.PgVector;
using CX.Engine.Assistants;
using CX.Engine.Assistants.AssessmentBuilder.Xml;
using CX.Engine.Assistants.ContextAI;
using CX.Engine.ChatAgents;
using CX.Engine.ChatAgents.OpenAI;
using CX.Engine.Common;
using CX.Engine.Common.Db;
using CX.Engine.Common.Embeddings;
using CX.Engine.Common.JsonSchemas;
using CX.Engine.Common.PostgreSQL;
using CX.Engine.Common.Stores.Json;
using CX.Engine.Common.Tracing;
using CX.Engine.Common.Tracing.Langfuse;
using CX.Engine.Common.Xml;
using CX.Engine.Importers;
using JetBrains.Annotations;
using Json.Path;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Scripting;
using Npgsql;

namespace CX.Engine.Assistants.AssessmentBuilder;

public class AssessmentAssistant : IAssistant, ISnapshottedOptions<AssessmentAssistant.Snapshot, AssessmentAssistantOptions, AssessmentAssistant>
{
    private readonly string _name;
    private readonly ILogger _logger;
    private readonly LangfuseService _langfuse;
    private readonly ContextAIService _contextAi;
    private readonly IServiceProvider _sp;
    public Snapshot CurrentShapshot { get; set; }
    public MonitoredOptionsSection<AssessmentAssistantOptions> OptionsSection { get; set; }

    public class Snapshot : Snapshot<AssessmentAssistantOptions, AssessmentAssistant>, ISnapshotSyncInit<AssessmentAssistantOptions>
    {
        public IChatAgent ChatAgent;
        public PostgreSQLClient Sql;
        public IChunkArchive Archive;
        public IStorageService StorageService;
        public IAssistant StrucredAssistant;
        
        public void Init(IConfigurationSection section, ILogger logger, IServiceProvider sp)
        {
            sp.GetRequiredNamedService(out ChatAgent, Options.ChatAgentName, section);
            sp.GetRequiredNamedService<IArchive, IChunkArchive>(out Archive, Options.ArchiveName, section);
            sp.GetRequiredNamedService(out StorageService, Options.StorageService, section);
            sp.GetRequiredNamedService(out StrucredAssistant, Options.AssistantName, section);

            if (Options.UseCrc32CachedChat)
            {
                if (ChatAgent is not OpenAIChatAgent oai)
                    throw new InvalidOperationException($"Crc32CachedChat only works with OpenAIChatAgent (for {nameof(Options.ChatAgentName)} of {section.Path})");

                Sql = sp.GetRequiredNamedService<PostgreSQLClient>(Options.PostgreSQLClientName);
                ChatAgent = new CachedChatAgent(sp.GetRequiredService<Crc32JsonStore>(), new(Sql, Options.CacheTableName), oai);
            }
        }
    }

    public AssessmentAssistant([NotNull] string name, MonitoredOptionsSection<AssessmentAssistantOptions> optionsSection, [NotNull] IServiceProvider sp,
        [NotNull] ILogger logger, LangfuseService langfuse, ContextAIService contextAiService)
    {
        _name = name ?? throw new ArgumentNullException(nameof(name));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _sp = sp ?? throw new ArgumentNullException(nameof(sp));
        _langfuse = langfuse;
        _contextAi = contextAiService;
        optionsSection.Bind<Snapshot, AssessmentAssistant>(this);
    }

    public async Task<bool> ExtractDataAsync()
    {
        var ss = CurrentShapshot;
        var opts = ss.Options;
        List<(Guid FileId, string Path, string Content, string Subject, int Grade, string Term, string ContentType)> allFiles = await ss.Sql.ListAsync(
            "SELECT fileid, file_path, content, subject, grade, term, content_type FROM sos_prototype_items ORDER BY fileid",
            rdr => (
                fileid: rdr.Get<Guid>("fileid"),
                path: rdr.Get<string>("file_path"),
                content: rdr.Get<string>("content"),
                subject: rdr.Get<string>("subject"),
                grade: rdr.Get<int>("grade"),
                term: rdr.Get<string>("term"),
                content_type: rdr.Get<string>("content_type")));

        var i = 0;
        var tasks = new List<Task>();
        foreach (var file in allFiles)
        {
            var origFileName = Path.GetFileName(file.Path);
            var newFileName = $"{i++}_{origFileName}";
            newFileName = Path.ChangeExtension(newFileName, ".txt");
            tasks.Add(File.WriteAllTextAsync(Path.Combine(opts.DiskImportPath, newFileName), file.Content));
            var metaFileName = newFileName + ".meta";
            var meta = new ImportJobMeta();
            meta.FileName = origFileName;
            meta.FileId = file.FileId.ToString();
            meta.Info = JsonSerializer.SerializeToDocument(new
            {
                Subject = file.Subject,
                Grade = file.Grade,
                Term = file.Term,
                ContentType = file.ContentType
            });
            tasks.Add(File.WriteAllTextAsync(Path.Combine(opts.DiskImportPath, metaFileName),
                JsonSerializer.Serialize(meta, new JsonSerializerOptions() { WriteIndented = true, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull })));
        }

        await tasks;

        return true;
    }

    public async Task<bool> DiskImportAsync()
    {
        var ss = CurrentShapshot;
        var opts = ss.Options;

        var diskImporter = _sp.GetRequiredService<DiskImporter>();

        Console.WriteLine("Clearing archive...");

        var archive = _sp.GetRequiredNamedService<IArchive>(opts.ArchiveName);
        await archive.ClearAsync();

        Console.WriteLine("Importing...");

        diskImporter.Options.LogProgressPerFile = 1; //only once done importing

        var cache = _sp.GetRequiredService<EmbeddingCache>();
        var busySaving = new SemaphoreSlim(1, 1);

        diskImporter.DocumentImported += async (_, _) =>
        {
            if (busySaving.Wait(0))
            {
                try
                {
                    await cache.SaveToFileAsync(opts.EmbeddingCachePath);
                }
                finally
                {
                    busySaving.Release();
                }
            }
        };
        await diskImporter.ImportAsync(new()
        {
            Archive = opts.ArchiveName,
            IsFileBased = false,
            DirectoryPath = opts.DiskImportPath,
            DirectoryPattern = "*.txt"
        });

        await cache.SaveToFileAsync(opts.EmbeddingCachePath);

        return true;
    }

    private async Task<string> GetAssessmentStructure(string userQuestion, AgentRequest astCtx)
    {
        return await CXTrace.Current.SpanFor("retrieve-structure").ExecuteAsync(async (_) =>
        {
            var ss = CurrentShapshot;
            var structuredAnswer = await ss.StrucredAssistant.AskAsync(userQuestion, astCtx);
            var root = JsonNode.Parse(structuredAnswer.Answer) ??
                       throw new NullReferenceException($"{nameof(ss.StrucredAssistant)} returned no JSON.");
            var matches = JsonPath.Parse(ss.Options.Document).Evaluate(root).Matches;
            var docNode = matches.FirstOrDefault() ?? throw new NullReferenceException("Document node is missing.");

            if (astCtx.EligibleForContextAi && !string.IsNullOrWhiteSpace(userQuestion))
                _contextAi.EnqueueAndForget(new LogThreadMessageRequest(astCtx.SessionId!, "user", astCtx.UserId!,
                    userQuestion));

            await using var cmd = new NpgsqlCommand(ss.Options.StructureQuery);
            cmd.Parameters.AddWithValue("id", docNode.Value.ToPrimitive());
            var res = await ss.Sql.ListStringAsync(cmd);
            return res.FirstOrDefault();
        });
    }
    
    private async Task<string> GetUrlMessage(string name, string assessment, string userQuestion, AgentRequest astCtx)
    {
        return await CXTrace.Current.SpanFor("url-intro-message").ExecuteAsync(async (_) =>
        {
            var ss = CurrentShapshot;
            var id = await ss.StorageService.InsertContentAsync($"{name}.txt", new MemoryStream(Encoding.UTF8.GetBytes(assessment)));
            var answer = await ss.StorageService.GetContentUrlAsync(id);
            var sb = new StringBuilder();
            sb.Append(userQuestion);
            sb.AppendLine();
            sb.Append(answer);
            var introReq = ss.ChatAgent.GetRequest(answer);
            introReq.SystemPrompt = ss.Options.IntroPrompt;
            introReq.History = astCtx.History;
            introReq.Question = sb.ToString();
            var introMessage = await ss.ChatAgent.RequestAsync(introReq);
            return introMessage.Answer;
        });
    }

    public Task<AssistantAnswer> AskAsync(string userQuestion, AgentRequest astCtx) =>
        CXTrace.TraceOrSpan(() => new CXTrace(_langfuse, astCtx.UserId, astCtx.SessionId).WithName(userQuestion).WithTags("assessment", _name),
            trace => trace.SpanFor($"assessment.{_name}", new { Question = userQuestion })).ExecuteAsync<AssistantAnswer>(async trace =>
        {
            //if (await ExtractDataAsync())
            //if (await DiskImportAsync())

            // var req = new ChunkArchiveRetrievalRequest()
            // {
            //     CutoffTokens = 9_000,
            //     MinSimilarity = 0.25,
            //     MaxChunks = 10,
            //     QueryString = userQuestion
            // };
            // req.Components.Add(new PgVectorAppendWhere() {
            //     Where = "AND metadata->'info'->'Grade' = '7' AND metadata->'info'->>'Subject' = 'English First Additional Language'"
            // });
            //
            // var matches = await ss.Archive.RetrieveAsync(req);
            //
            // var sb = new StringBuilder();
            // sb.AppendLine($"Matches: {matches.Count:#,##0}");
            // foreach (var match in matches)
            // {
            //     sb.AppendLine(JsonSerializer.Serialize(match.Chunk.Metadata.Info, new JsonSerializerOptions() { WriteIndented = true }).Indent(4));
            //     sb.AppendLine("------------------");
            // }
            //
            // return new(sb.ToString());

            var ss = CurrentShapshot;
            var doc = await GetAssessmentStructure(userQuestion, astCtx);

            var scope = new CxmlScope();
            scope["Snapshot"] = ss;
            var assessment = await CXTrace.Current.SpanFor("eval-assessment").ExecuteAsync(async (_) => await AssessmentCxml.EvalStringAsync(doc, scope));
            var answer = assessment?.Content;
            
            if (astCtx.EligibleForContextAi && !string.IsNullOrWhiteSpace(answer))
                _contextAi.EnqueueAndForget(new LogThreadMessageRequest(astCtx.SessionId!, "system", astCtx.UserId!,
                    answer.Preview(10_000)));

            
            trace.Output = new
            {
                Answer = answer
            };

            return new(await GetUrlMessage(assessment?.Name, answer, userQuestion, astCtx));
        });
}
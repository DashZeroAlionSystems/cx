using CX.Engine.Common;
using CX.Engine.Common.Stores.Json;
using CX.Engine.Common.Tracing;
using CX.Engine.Common.Tracing.Langfuse;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace CX.Engine.Assistants.CachedAssistants;

public class Crc32CachedAssistant : IAssistant, IDisposable
{
    private readonly string _name;
    private readonly IServiceProvider _sp;
    private readonly ILogger _logger;
    private readonly LangfuseService _langfuseService;
    private readonly Crc32JsonStore _jsonStore;
    private readonly IDisposable _optionsMonitorDisposable;
    private Snapshot _snapshot;
    
    private class Snapshot
    {
        public Crc32CachedAssistantOptions Options;
        public IAssistant UnderlyingAssistant;
        public Crc32JsonStore.StoreIdentifier StoreId;
    }

    private class CacheEntry
    {
        [JsonIgnore]
        public string Question;
        [JsonIgnore]
        public List<ChatAgents.ChatMessage> History;

        [JsonIgnore]
        [JsonProperty("Components")]
        [JsonConverter(typeof(ComponentsConverter<AgentOverride>))]
        public Components<AgentOverride> Overrides;

        public string Answer { get; set; }
        
        public string GetKey() => JsonSerializer.Serialize(new { Question = Question, History = History, Components = Overrides });
    }

    private void SetSnapshot(Crc32CachedAssistantOptions options)
    {
        var ss = new Snapshot();
        ss.Options = options;
        ss.UnderlyingAssistant = _sp.GetRequiredNamedService<IAssistant>(options.UnderlyingAssistantName);
        ss.StoreId = (options.CachePostgreSQLClientName, options.CacheTableName);
        _snapshot = ss;
    }

    public Crc32CachedAssistant([NotNull] string name, IOptionsMonitor<Crc32CachedAssistantOptions> monitor, [NotNull] ILogger logger, [NotNull] IServiceProvider sp, 
        [NotNull] LangfuseService langfuseService, [NotNull] Crc32JsonStore jsonStore)
    {
        _name = name ?? throw new ArgumentNullException(nameof(name));
        _sp = sp ?? throw new ArgumentNullException(nameof(sp));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _langfuseService = langfuseService ?? throw new ArgumentNullException(nameof(langfuseService));
        _jsonStore = jsonStore ?? throw new ArgumentNullException(nameof(jsonStore));
        _optionsMonitorDisposable = monitor.Snapshot(() => _snapshot?.Options, SetSnapshot, logger, sp);
    }

    public async Task<AssistantAnswer> AskAsync(string question, AgentRequest astCtx)
    {
        var ss = _snapshot;
        return await CXTrace.TraceOrSpan(() => new CXTrace(_langfuseService, astCtx.UserId, astCtx.SessionId).WithName(question).WithTags("crc32-cached").WithTags(_name),
            trace => trace.SpanFor($"crc32-cached.{_name}", new { Question = question })).ExecuteAsync(async section =>
        {
            var ce = new CacheEntry() { Question = question, History = astCtx.History, Overrides = astCtx.Overrides };
            var key = ce.GetKey();

            ce = await _jsonStore.GetAsync<CacheEntry>(ss.StoreId, key);
            if (ce != null)
            {
                section.Output = new { Cached = true, Answer = ce.Answer };
                return new(ce.Answer);
            }
            else
            {
                var res = await ss.UnderlyingAssistant.AskAsync(question, astCtx);
                ce = new();
                ce.Answer = res.Answer;
                await _jsonStore.SetAsync(ss.StoreId, key, ce);
                section.Output = new { Cached = false, Answer = res.Answer };
                return res;
            }
        });
    }

    public void Dispose()
    {
        _optionsMonitorDisposable?.Dispose();
    }
}
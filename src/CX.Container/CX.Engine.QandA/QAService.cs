using CX.Engine.Assistants;
using CX.Engine.ChatAgents;
using CX.Engine.ChatAgents.OpenAI;
using CX.Engine.Common;
using CX.Engine.FileServices;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CX.Engine.QAndA;

public class QAService : IDisposable
{
    public static string DefaultChatAgent = "GPT-4o-mini";

    private readonly string _name;
    private readonly ILogger _logger;
    private readonly IServiceProvider _sp;
    private readonly ChatCache _chatCache;
    private readonly FileService _fileService;
    private readonly IDisposable _optionsMonitorDisposable;

    private Snapshot _snapshot;

    private class Snapshot
    {
        public QAServiceOptions Options;
        public IAssistant Assistant;
        public OpenAIChatAgent Agent;
    }

    private void SetSnapshot(QAServiceOptions opts)
    {
        var ss = new Snapshot();
        ss.Options = opts;
        ss.Assistant = _sp.GetRequiredNamedService<IAssistant>(opts.AssistantName);
        ss.Agent = _sp.GetRequiredNamedService<IChatAgent>(opts.AgentName ?? DefaultChatAgent) as OpenAIChatAgent;
        if (ss.Agent == null)
            throw new InvalidOperationException($"Chat agent '{opts.AgentName}' is not an OpenAIChatAgent.");
        _snapshot = ss;
    }

    public QAService(string name, IOptionsMonitor<QAServiceOptions> monitor, [NotNull] ILogger logger, [NotNull] IServiceProvider sp, [NotNull] ChatCache chatCache,
        [NotNull] FileService fileService)
    {
        _name = name ?? throw new ArgumentNullException(nameof(name));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _sp = sp ?? throw new ArgumentNullException(nameof(sp));
        _chatCache = chatCache ?? throw new ArgumentNullException(nameof(chatCache));
        _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
        _optionsMonitorDisposable = monitor.Snapshot(() => _snapshot?.Options, SetSnapshot, logger, sp);
    }

    public void Dispose()
    {
        _optionsMonitorDisposable?.Dispose();
    }

    public async Task<MemoryStream> EvalAsync(Stream excelStream)
    {
        var ss = _snapshot;
        
        using var qaDoc = new QASession();
        qaDoc.LoadFromExcel(excelStream);
        var baseReq = new AgentRequest();
        _ = await qaDoc.EvaluateAsync(ss.Assistant, _chatCache, ss.Agent, baseReq, _fileService, _sp);
        var res = qaDoc.SaveToExcelStream();
        return res;
    }
}
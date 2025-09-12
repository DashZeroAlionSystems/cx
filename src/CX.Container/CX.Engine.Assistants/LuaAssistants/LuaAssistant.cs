using CX.Engine.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CX.Engine.Assistants.LuaAssistants;

public class LuaAssistant : IAssistant, IDisposable
{
    private Snapshot _snapshot;
    private readonly IServiceProvider _sp;
    private IDisposable _optionsMonitorDisposable;
    
    public class Snapshot
    {
        public LuaAssistantOptions Options;
        public LuaInstance LuaInstance;
    }

    private void SetOptions(LuaAssistantOptions opts)
    {
        var ss = new Snapshot();
        ss.Options = opts;
        ss.LuaInstance = _sp.GetRequiredNamedService<LuaCore>(opts.LuaCore).GetLuaInstance();
        _snapshot = ss;
    }

    public LuaAssistant(IOptionsMonitor<LuaAssistantOptions> options, ILogger logger, IServiceProvider sp)
    {
        _sp = sp ?? throw new ArgumentNullException(nameof(sp));
        _optionsMonitorDisposable = options.Snapshot(() => _snapshot?.Options, SetOptions, logger, sp); 
    }

    public async Task<AssistantAnswer> AskAsync(string question, AgentRequest astCtx)
    {
        var ss = _snapshot;
        var res = await ss.LuaInstance.RunAsync(question);
        return new(res);
    }

    public void Dispose()
    {
        _optionsMonitorDisposable?.Dispose();
    }
}
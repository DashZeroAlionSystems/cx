using CX.Engine.Assistants.Channels;
using CX.Engine.ChatAgents;
using CX.Engine.ChatAgents.OpenAI;
using CX.Engine.Common;
using CX.Engine.Common.Tracing;
using CX.Engine.Common.Tracing.Langfuse;
using Cx.Engine.Common.PromptBuilders;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CX.Engine.Assistants.MenuAssistants;

public class MenuAssistant : IAssistant, IDisposable
{
    private readonly string _name;
    private readonly ILogger _logger;
    private readonly IServiceProvider _sp;
    private readonly IDisposable _optionsMonitorDisposable;
    private readonly LangfuseService _langfuseService;
    
    private string FullName => "menu." + _name;
    private Snapshot _snapshot;
    
    private readonly AssistantOpContext<Snapshot> OpContext;

    private class Snapshot
    {
        public MenuAssistantOptions Options;
        public OpenAIChatAgent ChatAgent;
    }

    private void SetSnapshot(MenuAssistantOptions options)
    {
        var ss = new Snapshot();
        ss.Options = options;
        ss.ChatAgent = (OpenAIChatAgent)_sp.GetRequiredNamedService<IChatAgent>(options.ChatAgentName);
        _snapshot = ss;
    }

    public MenuAssistant(string name, IOptionsMonitor<MenuAssistantOptions> optionsMonitor, [NotNull] ILogger logger, [NotNull] IServiceProvider sp, [NotNull] LangfuseService langfuseService)
    {
        _name = name ?? throw new ArgumentNullException(nameof(name));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _sp = sp ?? throw new ArgumentNullException(nameof(sp));
        _langfuseService = langfuseService ?? throw new ArgumentNullException(nameof(langfuseService));
        OpContext = new(() => _snapshot);
        _optionsMonitorDisposable = optionsMonitor.Snapshot(() => _snapshot?.Options, SetSnapshot, logger, sp);
    }

    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public class AnswerClass
    {
        public string Reasoning { get; set; }
        public string Answer { get; set; }
        public string AgentId { get; set; }
        public string QuestionToAskOtherAgent { get; set; }
    }

    public async Task<AssistantAnswer> AskAsync(string question, AgentRequest astCtx)
    {
        AssistantsSharedAsyncLocal.EnterAsk();
        
        var ss = _snapshot;
        var opts = ss.Options;
        var opCtx = OpContext.Local.Value = new();
        var agent = ss.ChatAgent;
        
        opCtx.FeedbackSlimlock = astCtx.FeedbackLock;
        opCtx.Snapshot = ss;

        var section = CXTrace.TraceOrSpan(() => new CXTrace(_langfuseService, astCtx.UserId, astCtx.SessionId).WithTags("menu").WithTags(_name).WithName(question),
            trace => trace.SpanFor(FullName, new { Question = question, SystemPropmt = ss.Options.Instructions }));

        return await section.ExecuteAsync(async _ =>
        {
            var req = agent.GetRequest(question);
            var schema = agent.GetSchema("menu_assistant");
            schema.Object.AddPropertiesFrom<AnswerClass>();

            if (!opts.EnableReasoning)
                schema.Object.Properties.Remove(nameof(AnswerClass.Reasoning));
            
            if (!opts.EnableQuestionRephrase)
                schema.Object.Properties.Remove(nameof(AnswerClass.QuestionToAskOtherAgent));

            void AddOptionId(string id) => schema.Object.Properties[nameof(AnswerClass.AgentId)].Choices.Add(id);
            
            if (opts.HasNoneOption)
                AddOptionId("None");
            
            req.SetResponseSchema(schema);

            var pb = new PromptBuilder();
            if (opts.HasNoneOption)
                pb.Add(opts.NonePrompt, opts.NonePriority);
            
            pb.Add(ss.Options.Instructions, opts.InstructionsPriority);
            pb.Add(ss.Options.DataStructurePrompt, opts.DataStructurePriority);

            foreach (var mo in ss.Options.Options)
            {
                pb.Add(mo.Prompt, mo.Priority);
                AddOptionId(mo.AgentId);
            }

            req.SystemPrompt = pb.GetPrompt();
            
            var res = await agent.RequestAsync<AnswerClass>(req);
            var answer = res.Answer;

            var optionId = res.AgentId;

            if (optionId != "None")
            {
                var option = opts.Options.FirstOrDefault(o => o.AgentId == optionId);
                if (option == null)
                    throw new InvalidOperationException($"Option '{optionId}' not found.");

                var otherQuestion = question;
                if (!string.IsNullOrWhiteSpace(res.QuestionToAskOtherAgent))
                    otherQuestion = res.QuestionToAskOtherAgent;
                
                var channel = _sp.GetRequiredNamedService<Channel>(option.ChannelName);
                var cRes = await channel.Assistant.AskAsync(otherQuestion, astCtx);
                section.Output = new
                {
                    SystemPrompt = req.SystemPrompt,
                    QuestionToAskOtherAgent = res.QuestionToAskOtherAgent,
                    OptionId = optionId,
                    Answer = answer
                };
                return cRes;
            }

            section.Output = new
            {
                SystemPrompt = req.SystemPrompt,
                Reasoning = res.Reasoning,
                OptionId = optionId,
                Answer = answer
            };
            
            return new(answer);
        });
   }

    public void Dispose()
    {
        _optionsMonitorDisposable?.Dispose();
    }
}
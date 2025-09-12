using CX.Engine.Archives;
using CX.Engine.Assistants.ContextAI;
using CX.Engine.ChatAgents;
using CX.Engine.ChatAgents.OpenAI;
using CX.Engine.Common;
using CX.Engine.Common.Tracing;
using CX.Engine.Common.Tracing.Langfuse;
using CX.Engine.TextProcessors;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

// ReSharper disable InconsistentlySynchronizedField

namespace CX.Engine.Assistants.Walter1;

public class Walter1Assistant : IAssistant, IUsesArchive, IDisposable
{
    private readonly string _name;
    private readonly ChatCache _chatCache;
    private readonly LangfuseService _langfuseService;
    private readonly ContextAIService _contextAiService;
    private Walter1AssistantOptions _options;
    private readonly IServiceProvider _sp;
    private readonly IDisposable _optionsChangeDisposable;

    public IChunkArchive ChunkArchive => _sp.GetRequiredNamedService<IChunkArchive>(_options.Archive);

    public Walter1Assistant(string name, ChatCache chatCache, IOptionsMonitor<Walter1AssistantOptions> options, IServiceProvider sp,
        LangfuseService langfuseService, ContextAIService contextAIService, ILogger logger)
    {
        _name = name ?? throw new ArgumentNullException(nameof(name));
        _chatCache = chatCache ?? throw new ArgumentNullException(nameof(chatCache));
        _langfuseService = langfuseService ?? throw new ArgumentNullException(nameof(langfuseService));
        _contextAiService = contextAIService ?? throw new ArgumentNullException(nameof(contextAIService));

        _optionsChangeDisposable = options.Snapshot(() => _options, o => { _options = o; }, logger, sp);

        _sp = sp ?? throw new ArgumentNullException(nameof(sp));
        _options.Validate();
    }

    public string SystemPrompt { get; set; }
    public string ContextualizePrompt { get; set; }

    private List<IChunkArchive> ResolveArchives(Walter1AssistantOptions options)
    {
        var res = new List<IChunkArchive>();

        if (!string.IsNullOrWhiteSpace(options.Archive))
            res.Add(_sp.GetRequiredNamedService<IChunkArchive>(options.Archive));

        if (options.Archives != null)
        {
            foreach (var archive in options.Archives)
                if (!string.IsNullOrWhiteSpace(archive))
                    res.Add(_sp.GetRequiredNamedService<IChunkArchive>(archive));
        }

        return res;
    }

    public async Task<AssistantAnswer> AskAsync(string question, AgentRequest astCtx)
    {
        AssistantsSharedAsyncLocal.EnterAsk();
        
        var opts = _options.Clone();
        SchemaResponseFormat responseFormatOverride = null;

        foreach (var comp in astCtx.Overrides)
        {
            if (comp is Walter1AssistantOptionsOverrides over)
            {
                if (over.AddArchives != null)
                    if (opts.Archives == null)
                        opts.Archives = over.AddArchives.ToList();
                    else
                        opts.Archives = opts.Archives.Union(over.AddArchives, StringComparer.InvariantCultureIgnoreCase).ToList();

                if (over.RemoveArchives != null && opts.Archives != null) opts.Archives = opts.Archives.Except(over.RemoveArchives).ToList();
            }
            
            if (comp is ResponseFormatOverride rfo)
                responseFormatOverride = rfo.ResponseFormat;
        }

        opts.Validate();

        var archives = ResolveArchives(opts);

        if (astCtx.EligibleForContextAi && !string.IsNullOrWhiteSpace(question))
            _contextAiService.EnqueueAndForget(new LogThreadMessageRequest(astCtx.SessionId!, "user", astCtx.UserId!,
                question));

        var section = CXTrace.TraceOrSpan(() => new CXTrace(_langfuseService, astCtx.UserId, astCtx.SessionId)
            .WithName(question.Preview(50))
            .WithTags("walter-1", "ask", _name), trace => trace.SpanFor($"walter-1.{_name}", new {Question = question}));
        
        var res = new AssistantAnswer();

        var iagent = _sp.GetRequiredNamedService<IChatAgent>(opts.ChatAgent);
        var agent = iagent as OpenAIChatAgent;

        if (agent == null)
            throw new InvalidOperationException($"{nameof(Walter1Assistant)} only supports {nameof(OpenAIChatAgent)}"); 
        var chatCtx = agent.GetRequest(question);
        chatCtx.SystemPrompt = SystemPrompt ?? opts.DefaultSystemPrompt;
        chatCtx.ContextualizePrompt = ContextualizePrompt ?? opts.DefaultContextualizePrompt;
        if (responseFormatOverride != null)
            chatCtx.ResponseFormatBase = responseFormatOverride;

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
                chatCtx.History.Insert(0, h);
            }
        }

        await section
            .WithInput(new
            {
                Question = chatCtx.Question,
                SystemPrompt = chatCtx.SystemPrompt,
                History = chatCtx.History,
                UseAttachments = opts.UseAttachments
            })
            .ExecuteAsync(async _ =>
            {
                try
                {
                    question = await TextProcessingDI.ProcessAsync(question, _sp, opts.InputProcessors);
                }
                catch (TextValidationException ex)
                {
                    res.TextValidationException = ex;
                    res.Answer = "I will not answer your question since it contains " + ex.Message;
                    res.IsRefusal = true;
                    astCtx.Record(question, res);
                    section.Output = new
                    {
                        Rejected = true,
                        TextValidationException = res.TextValidationException.Message,
                        Answer = res.GetCellContent()
                    };
                    return res;
                }

                chatCtx.Question = question;

                string embeddingLookup;
                var contextualizePrompt = ContextualizePrompt ?? opts.DefaultContextualizePrompt;
                if (!string.IsNullOrWhiteSpace(contextualizePrompt))
                {
                    var resolveCtx = agent.GetRequest(question);
                    resolveCtx.SystemPrompt = chatCtx.SystemPrompt + "\r\n" + contextualizePrompt;
                    resolveCtx.History.AddRange(chatCtx.History);
                    embeddingLookup = (await _chatCache.ChatAsync(resolveCtx, astCtx.UseCache)).Answer;
                    if (string.IsNullOrWhiteSpace(embeddingLookup))
                        embeddingLookup = chatCtx.GetQueryEmbeddingString();
                }
                else
                    embeddingLookup = chatCtx.GetQueryEmbeddingString();

                res.EmbeddingLookup = embeddingLookup;

                List<ArchiveMatch> matches = new();

                async Task GetMatchesAsync(IChunkArchive archive)
                {
                    var res = await archive.RetrieveAsync((embeddingLookup,
                        opts.MinSimilarity,
                        opts.CutoffContextTokens,
                        opts.MaxChunksPerAsk));

                    lock (matches)
                        matches.AddRange(res);
                }

                await (from archive in archives select GetMatchesAsync(archive));

                matches = BaseChunkArchive.OrderAndApplyTokenCutoffAndMaxChunks(matches, opts.CutoffContextTokens, opts.MaxChunksPerAsk);

                if (opts.TopDocumentLimit.HasValue)
                    matches = Walter1Helpers.TopKDocuments(res, matches, opts.TopDocumentLimit.Value);

                if (opts.SortChunks)
                    matches = matches.OrderBy(m => m.Chunk.Metadata.SourceDocumentGroup)
                        .ThenBy(m => m.Chunk.Metadata.SourceDocument)
                        .ThenBy(m => m.Chunk.SeqNo)
                        .ToList();

                chatCtx.Chunks.AddRange(matches.Select(m => m.Chunk).ToList());

                var rank = 1;
                foreach (var match in matches.OrderByDescending(m => m.Score))
                {
                    res.Chunks.Add(new()
                    {
                        Content = match.Chunk.GetContextString(),
                        Rank = rank,
                        Similarity = match.Score
                    });
                    rank++;
                }

                var atts = new List<AttachmentInfo>();
                if (opts.UseAttachments)
                    foreach (var chunk in chatCtx.Chunks)
                    {
                        var attachments = chunk.Metadata.GetAttachments(false);
                        if (attachments != null)
                        {
                            foreach (var att in attachments)
                            {
                                if (!atts.Any(ai => ai.IsSameAttachment(att)))
                                {
                                    var lAtt = att.Clone();
                                    atts.Add(lAtt);
                                }
                            }
                        }
                    }

                if (atts.Count > 0)
                {
                    foreach (var att in atts)
                    {
                        chatCtx.Attachments.Add(att);
                        res.InputAttachments.Add(att);
                    }
                }

                chatCtx.UseAttachments = opts.UseAttachments;
                var chatAnswer = await _chatCache.ChatAsync(chatCtx, astCtx.UseCache);
                res.Answer = chatAnswer.Answer;

                if (astCtx.EligibleForContextAi && !string.IsNullOrWhiteSpace(chatAnswer.Answer))
                {
                    _contextAiService.EnqueueAndForget(new LogThreadMessageRequest(astCtx.SessionId!,
                        "assistant",
                        astCtx.UserId!,
                        chatAnswer.Answer));
                }

                astCtx.Record(question, res, chatAnswer.ToolCalls);

                if (chatAnswer.Attachments?.Length > 0)
                {
                    res.Attachments ??= new();
                    foreach (var att in chatAnswer.Attachments)
                    {
                        if (astCtx.EligibleForContextAi && att.FileName != null)
                        {
                            _contextAiService.EnqueueAndForget(new LogThreadToolUseRequest(astCtx.SessionId!,
                                "assistant",
                                att.FileName,
                                astCtx.UserId!));
                        }

                        res.Attachments.Add(att);
                    }
                }

                section.Output = new
                {
                    Rejected = false,
                    EmbeddingLookup = embeddingLookup,
                    Answer = res.GetCellContent()
                };
                return res;
            });

        res.SystemPrompt = chatCtx.SystemPrompt;
        return res;
    }

    public void Dispose()
    {
        _optionsChangeDisposable?.Dispose();
    }
}
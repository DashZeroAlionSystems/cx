using CX.Engine.Archives.InMemory;
using CX.Engine.ChatAgents;
using CX.Engine.Common;
using CX.Engine.TextProcessors.Splitters;
using CX.Engine.Assistants;
using CX.Engine.ChatAgents.OpenAI;

namespace CX.Engine.QAndA.Auto;

public class AutoQA
{
    public readonly AutoQAOptions Options;

    private readonly ChatCache _chatCache;
    private readonly InMemoryChunkArchive _chunkArchive;
    private readonly IAssistant _assistant;
    private readonly IServiceProvider _sp;

    public AutoQA(AutoQAOptions options, ChatCache chatCache, IServiceProvider sp)
    {
        Options = options ?? throw new ArgumentNullException(nameof(options));
        _chatCache = chatCache ?? throw new ArgumentNullException(nameof(chatCache));
        _sp = sp ?? throw new ArgumentNullException(nameof(sp));
        Options.Validate();
        _chunkArchive = sp.GetRequiredNamedService<InMemoryChunkArchive>(options.MemoryArchive);
        _assistant = sp.GetRequiredNamedService<IAssistant>(options.Assistant);
    }

    public async Task<(string question, TextChunk chunk)> GenerateQuestionAsync()
    {
        var chunk = await _chunkArchive.GetRandomChunkAsync();
        var agent = (OpenAIChatAgent)_sp.GetRequiredNamedService<IChatAgent>(Options.ChatAgent);
        var req = agent.GetRequest(chunk.Content, systemPrompt: Options.QuestionPrompt);
        var atts = chunk.Metadata.GetAttachments(false);
        if (atts != null)
            req.Attachments.AddRange(atts);
        var res = await _chatCache.ChatAsync(req, useCache: false);
        return (res.Answer, chunk);
    }

    public async Task<string> GenerateEvalsAsync(string answer)
    {
        if (answer == null)
            return "";

        
        var agent = (OpenAIChatAgent)_sp.GetRequiredNamedService<IChatAgent>(Options.ChatAgent);
        var req = agent.GetRequest(answer, systemPrompt: Options.EvalPrompt);
        var res = await _chatCache.ChatAsync(req);

        return res.Answer;
    }

    public async Task PopulateQADocAsync(QASession session, int qty, Action questionGenerated)
    {
        var tasks = new List<Task>();
        for (var i = 0; i < qty; i++)
        {
            async Task ProcessQuestion()
            {
                var entry = new QAEntry();
                var qRes = await GenerateQuestionAsync();

                lock (session.Entries)
                    if (session.Entries.Any(e => e.Question == qRes.question))
                        return;

                entry.Question = qRes.question;

                var aRes = await _assistant.AskAsync(entry.Question, new AgentRequest().HasNoCaching());
                entry.Answer = aRes;

                var criteria = await GenerateEvalsAsync(aRes.Answer);
                entry.Criteria.AddRange(criteria.Split('\n')
                    .Select(crit =>
                    {
                        if (crit.StartsWith("- ")) crit = crit[2..];
                        return new NLCriteria(crit);
                    }));

                lock (session.Entries)
                    session.Entries.Add(entry);

                questionGenerated?.Invoke();
            }

            tasks.Add(ProcessQuestion());
        }

        await Task.WhenAll(tasks);
    }
}
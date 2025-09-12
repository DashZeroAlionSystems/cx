using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using CX.Engine.Archives;
using CX.Engine.Archives.InMemory;
using CX.Engine.ChatAgents;
using CX.Engine.Common;
using CX.Engine.Common.Meta;
using CX.Engine.Common.Tracing;
using CX.Engine.Common.Tracing.Langfuse;
using CX.Engine.DocExtractors.Text;
using CX.Engine.FileServices;
using CX.Engine.Importers;
using CX.Engine.TextProcessors.Splitters;
using CX.Engine.Assistants;
using CX.Engine.ChatAgents.OpenAI;
using CX.Engine.Common.Json;
using CX.Engine.QAndA;
using CX.Engine.QAndA.Auto;
using Discord;
using Discord.Interactions;
using Discord.Rest;
using Discord.WebSocket;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ArgumentNullException = System.ArgumentNullException;

namespace CX.Engine.Discord;

// Keep in mind your module **must** be public and inherit ModuleBase.
// If it isn't, it will not be discovered by AddModulesAsync!
[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class AssistantModule : InteractionModuleBase
{
    private readonly ChatCache _chatCache;
    private readonly ILogger<AssistantModule> _logger;
    private readonly LangfuseService _langfuseService;
    private readonly IServiceProvider _sp;
    private readonly FileService _fileService;
    private readonly PDFPlumber _pdfPlumber;
    private readonly PythonDocX _pythonDocX;
    private readonly LineSplitter _lineSplitter;
    private readonly IChunkArchive _chunkArchive;
    private readonly QASession _qaSession = new();

    public UserContext UserContext => UserContexts.Get(Context.User.Id,
        (Context.User as SocketGuildUser)?.Nickname ?? Context.User.GlobalName ?? Context.User.Username);
    
    public new DiscordServiceInteractionContext Context => (DiscordServiceInteractionContext)base.Context;

    private readonly SemaphoreSlim _importLock = new(1, 1);

    private async Task<AutoQA> GetAutoQAForChannelAsync()
    {
        var opts = ChannelOptions;
        
        var memoryArchiveName = ((opts.Assistant as IUsesArchive)?.ChunkArchive as InMemoryChunkArchive)?.Options.Name;
        
        if (memoryArchiveName == null)
        {
            await SendTextAsync("Quiz is only supported for memory bots");
            return null;
        }

        if (string.IsNullOrWhiteSpace(opts.QuizPrompt))
        {
            await SendTextAsync("Quiz bot is not configured for this channel");
            return null;
        }

        var autoQa = new AutoQA(new()
            {
                Assistant = opts.AssistantName,
                QuestionPrompt = opts.QuizPrompt,
                EvalPrompt =
                    "I want to mark answers like this one.  Mention as many facts contained in this answer as you can formulated as 'this answer should contain ...'.  Output should be a bullet list: Each criteria should be in its own line and start with a dash and a space.",
                MemoryArchive = memoryArchiveName
            },
            _chatCache,
            _sp);
        return autoQa;
    }

    public AssistantModule(ChatCache chatCache, ILogger<AssistantModule> logger, FileService fileService,
        PDFPlumber pdfPlumber, PythonDocX pythonDocX, LineSplitter lineSplitter, IServiceProvider sp, IOptions<DiskImporterOptions> options,
        LangfuseService langfuseService)
    {
        _chatCache = chatCache ?? throw new ArgumentNullException(nameof(chatCache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
        _pdfPlumber = pdfPlumber ?? throw new ArgumentNullException(nameof(pdfPlumber));
        _pythonDocX = pythonDocX ?? throw new ArgumentNullException(nameof(pythonDocX));
        _lineSplitter = lineSplitter ?? throw new ArgumentNullException(nameof(lineSplitter));
        _langfuseService = langfuseService ?? throw new ArgumentNullException(nameof(langfuseService));
        _sp = sp ?? throw new ArgumentNullException(nameof(sp));

        options.Value.Validate();
        _chunkArchive = sp.GetRequiredNamedService<IChunkArchive>(options.Value.Archive);
    }

    private async Task<ITextChannel> GetTextChannelAsync() =>
        (ITextChannel)await Context.Client.GetChannelAsync(Context.Interaction.ChannelId!.Value);

    private async Task SendTextAsync(string s, bool ephemeral = false)
    {
        if (string.IsNullOrWhiteSpace(s))
            return;

        var isResponse = !Context.Interaction.HasResponded;
        var isFile = s.Length > 1_000;

        if (isResponse)
        {
            if (isFile)
                await RespondWithFileAsync(new MemoryStream(Encoding.UTF8.GetBytes(s)), "response.txt", ephemeral: ephemeral);
            else
                await RespondAsync(s, ephemeral: ephemeral);
        }
        else
        {
            var channel = await GetTextChannelAsync();
            if (isFile)
                await channel.SendFileAsync(new FileAttachment(new MemoryStream(Encoding.UTF8.GetBytes(s)), "response.txt"));
            else
                await channel.SendMessageAsync(s);

            // if (isFile)
            //     await FollowupWithFileAsync(new MemoryStream(Encoding.UTF8.GetBytes(s)), "response.txt", ephemeral: ephemeral);
            // else
            //     await FollowupAsync(s, ephemeral: ephemeral);
        }
    }

    private async Task SendFileAsync(Stream file, string filename, string text = null)
    {
        var isResponse = !Context.Interaction.HasResponded;
        if (isResponse)
            await RespondWithFileAsync(file, filename);
        else
        {
            var channel = await GetTextChannelAsync();
            await channel.SendFileAsync(file, filename, text);
        }
    }


    private async Task SendExceptionAsync(Exception ex)
    {
        await SendTextAsync("Error! " + ex.GetType() + ":\r\n" + ex.Message);
    }

    private bool ShouldProcess()
    {
        if (Context.User.IsBot)
            return false;

        if (Context.User.IsWebhook)
            return false;

        var opts = ChannelOptions;

        if (opts == null)
            return false;

        if (!opts.Active)
            return false;

        return true;
    }

    public ulong ChannelId => Context.Interaction.ChannelId!.Value;

    public DiscordChannelOptions ChannelOptions =>
        Context.Snapshot.Options.GetChannelOptionsByDiscordId(ChannelId);

    public IAssistant Assistant => ChannelOptions.Assistant;

    public AgentRequest AgentRequest
    {
        get
        {
            var res = UserContext.GetAssistantContextForChannel(ChannelId);
            return res;
        }
    }

    [SlashCommand("forget", "Forgets the user's conversation history in this channel.")]
    public async Task ForgetAsync()
    {
        AgentRequest.ForgetHistory();
        await SendTextAsync("Conversation history for this user on this channel has been wiped.");
    }

    [SlashCommand("quiz", "Asks the Bot to ask itself a question and answer it.")]
    public async Task QuizAsync()
    {
        if (!ShouldProcess())
            return;
        
        await SendTextAsync("Busy...");

        var autoQa = await GetAutoQAForChannelAsync();

        if (autoQa == null)
            return;

        var qRes = await autoQa.GenerateQuestionAsync();
        await SendTextAsync($"**Question:** {qRes.question}");

        await AskAsync(qRes.question);
    }

    [SlashCommand("ask", "Asks CX Bot a question.")]
    public async Task AskAsync([Summary("question", "The question you want to ask")] string question)
    {
        if (!ShouldProcess())
            return;

        await SendTextAsync("Busy...");

        try
        {
            UserContext.Clear();
            UserContext.Question = question;

            var reply = await Assistant.AskAsync(question, AgentRequest);
            if (string.IsNullOrWhiteSpace(reply.Answer))
                reply.Answer = null;

            UserContext.Answer = reply.Answer;
            await SendTextAsync(reply.Answer ?? "<no answer text>");

            if (reply.Attachments != null)
            {
                foreach (var attachment in reply.Attachments)
                {
                    var stream = await _fileService.GetContentStreamAsync(attachment);
                    if (stream == null)
                    {
                        await SendTextAsync(
                            $"Error processing attachment (no way to get content): {JsonSerializer.Serialize(attachment)}");
                        continue;
                    }

                    await SendFileAsync(stream, attachment.FileName);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during ask");
            await SendExceptionAsync(ex);
            throw;
        }
    }

    [SlashCommand("ask_many", "Asks CX Bot a question and gets multiple results back.")]
    public async Task AskManyAsync([Summary("question", "The question you want to ask")] string question,
        [Summary("number_results", "The number of results you want to get back (2 - 10)")]
        int results)
    {
        if (!ShouldProcess())
            return;

        if (results < 2 || results > 10)
        {
            await SendTextAsync("Results must be between 2 and 10.");
            return;
        }

        await SendTextAsync("Busy...");

        try
        {
            for (var i = 0; i < results; i++)
            {
                UserContext.Clear();
                UserContext.Question = question;
                using var ctx = AgentRequest.GetScoped().HasNoCaching().DoesNotUpdateHistory();
                var reply = await Assistant.AskAsync(question, ctx);
                UserContext.Answer = reply.Answer;
                await SendTextAsync(reply.Answer ?? "<no answer text>");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during askmany");
            await SendExceptionAsync(ex);
            throw;
        }
    }

    [SlashCommand("eval_last_doc", "Evaluates the last document uploaded in this channel (must be one of the last 100 messages).")]
    public async Task EvalLastDocAsync(
        [Summary("use_chat_cache", "Whether to use the chat cache to speed up the evaluation.")]
        bool useChatCache,
        [Summary("exportToExcel", "True if an output Excel file should be generated")]
        bool exportToExcel,
        [Summary("exportToWord", "True if an output Word  file should be generated")]
        bool exportToWord)
    {
        if (!ShouldProcess())
            return;

        var start = Stopwatch.StartNew();

        await SendTextAsync("Scanning message history...");

        try
        {
            var channel = await Context.Client.GetChannelAsync(Context.Interaction.ChannelId!.Value);

            IAttachment at = null;
            foreach (var msg in (await ((RestTextChannel)channel).GetMessagesAsync().FlattenAsync()).OrderByDescending(m => m.Id))
            {
                if (msg.Attachments.Count == 0)
                    continue;

                at = msg.Attachments.FirstOrDefault(a => Path.GetExtension(a.Filename) == ".xlsx");

                if (at != null)
                    break;
            }

            if (at == null)
            {
                await SendTextAsync("No XLSX document found in last 100 messages.");
                return;
            }

            await SendTextAsync("Loading attachment...");
            var url = at.Url;
            using var client = new HttpClient();
            await using var stream = await client.GetStreamAsync(url);
            var doc = new QASession();
            doc.LoadFromExcel(stream);

            await SendTextAsync("Evaluating...");
            using var ctx = AgentRequest.GetScoped();
            ctx.UseCache = useChatCache;
            ctx.HasNoHistory();
            var gpt4omini = (OpenAIChatAgent)_sp.GetRequiredNamedService<IChatAgent>("OpenAI.GPT-4o-mini");
            var busy = doc.EvaluateAsync(Assistant, _chatCache, gpt4omini, ctx, _fileService, _sp);

            while (!busy.IsCompleted)
            {
                await Task.WhenAny(busy, Task.Delay(TimeSpan.FromSeconds(300)));
                if (!busy.IsCompleted)
                    await SendTextAsync($"Busy evaluating... {doc.CompletedEntries:#,##0} / {doc.Entries.Count:#,##0} questions");
            }

            await busy;

            await SendTextAsync($"Q&A evaluated in {start.Elapsed}");

            if (exportToWord)
                await SendFileAsync(doc.SaveToWord(),
                    at.Filename.Replace(".xlsx", ".docx"),
                    $"Here is the exported Excel document.");

            if (exportToExcel)
                await SendFileAsync(doc.SaveToExcelStream(),
                    at.Filename,
                    $"Here is the exported Word document.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error during eval_last_doc");
            await SendExceptionAsync(ex);
            throw;
        }
    }

    [SlashCommand("eval", "Evaluate the last answer according to a criteria.")]
    public async Task EvalAsync(string criteria)
    {
        if (!ShouldProcess())
            return;

        try
        {
            if (UserContext.Answer == null)
            {
                await SendTextAsync("No answer to eval.");
                return;
            }

            await SendTextAsync("Evaluating...");

            var qaEntry = new QAEntry();

            var gpt4omini = _sp.GetRequiredNamedService<OpenAIChatAgent>("OpenAI.GPT-4o-mini");
            var res = await _qaSession.EvalQuestionWithNLAsync(UserContext.Answer, criteria, _chatCache, gpt4omini, qaEntry);
            await SendTextAsync(res.detail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during eval");
            await SendExceptionAsync(ex);
            throw;
        }
    }

    [SlashCommand("eval_regex", "Asks CX Bot a question.")]
    public async Task EvalRegexAsync(string regex)
    {
        if (!ShouldProcess())
            return;

        try
        {
            var rx = new Regex(regex);

            if (UserContext.Chunks == null)
            {
                await SendTextAsync("No chunks to eval.");
                return;
            }

            var pass = false;
            foreach (var chunk in UserContext.Chunks)
            {
                if (rx.IsMatch(chunk.Content))
                {
                    pass = true;
                    break;
                }
            }

            await SendTextAsync(pass ? "Pass" : "Fail");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during eval_regex");
            await SendExceptionAsync(ex);
            throw;
        }
    }

    [SlashCommand("import_last_doc", "Imports the last document uploaded in this channel (must be one of the last 100 messages).")]
    public async Task ImportLastDocAsync()
    {
        if (!ShouldProcess())
            return;

        var start = Stopwatch.StartNew();

        await SendTextAsync("Scanning message history...");

        await CXTrace.GetImportTrace(_langfuseService)
            .WithName("Discord Import")
            .WithInput(new
            {
                SourceDocument = "Discord Attachment"
            })
            .ExecuteAsync(async trace =>
            {
                try
                {
                    var channel = await Context.Client.GetChannelAsync(Context.Interaction.ChannelId!.Value);
                    var extensions = new[] { ".pdf", ".docx" };

                    IAttachment at = null;
                    foreach (var msg in (await ((RestTextChannel)channel).GetMessagesAsync().FlattenAsync()).OrderByDescending(m => m.Id))
                    {
                        if (msg.Attachments.Count == 0)
                            continue;

                        at = msg.Attachments.FirstOrDefault(a => extensions.Contains(Path.GetExtension(a.Filename)));

                        if (at != null)
                            break;
                    }

                    if (at == null)
                    {
                        await SendTextAsync($"No importable document found in last 100 messages ({string.Join(" ", extensions)}).");
                        return;
                    }

                    await SendTextAsync("Loading attachment...");
                    var url = at.Url;
                    using var client = new HttpClient();
                    await using var stream = await client.GetStreamAsync(url);

                    await using var ms = await stream.CopyToMemoryStreamAsync();

                    await SendTextAsync("Importing...");

                    var ext = Path.GetExtension(at.Filename);

                    var docMeta = new DocumentMeta();
                    docMeta.SourceDocument = at.Filename;
                    docMeta.Id = Guid.NewGuid();

                    string content;
                    if (ext == ".pdf")
                        content = await _pdfPlumber.ExtractToTextAsync(ms, docMeta);
                    else
                        content = await _pythonDocX.ExtractToTextAsync(ms, docMeta);

                    await _importLock.WaitAsync();
                    try
                    {
                        var req = new LineSplitterRequest(content, docMeta);
                        await _chunkArchive.ImportAsync(docMeta.Id.Value, await _lineSplitter.ChunkAsync(req));
                    }
                    finally
                    {
                        _importLock.Release();
                    }

                    await SendTextAsync($"Imported {at.Filename} in {start.Elapsed}.");
                    trace.Output = "Success";
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error during import_last_doc");
                    await SendExceptionAsync(ex);
                    throw;
                }
            });
    }

    [SlashCommand("trace", "Asks CX Bot to trace the last question.")]
    public async Task TraceAsync()
    {
        if (!ShouldProcess())
            return;

        if (UserContext.Question == null)
        {
            await SendTextAsync("No question to trace.");
            return;
        }

        await SendTextAsync("Busy...");

        try
        {
            using var ctx = AgentRequest
                .GetScoped()
                .DoesNotUpdateHistory()
                .RemoveLastQuestionAndAnswerFromHistory();
            var reply = await Assistant.AskAsync(UserContext.Question, ctx);
            UserContext.Answer = reply.Answer;
            UserContext.Chunks = reply.Chunks;
            var sb = new StringBuilder();
            sb.AppendLine("--- QUESTION ---");
            sb.AppendLine(UserContext.Question);
            sb.AppendLine();
            sb.AppendLine("--- ANSWER ---");
            sb.AppendLine(reply.Answer);
            sb.AppendLine();
            sb.AppendLine("--- ATTACHMENTS FINAL ---");
            if (reply.Attachments != null)
                foreach (var att in reply.Attachments)
                {
                    sb.AppendLine(att.GetJsonString());
                }

            sb.AppendLine();
            sb.AppendLine("--- SYSTEM PROMPT ---");
            sb.AppendLine(reply.SystemPrompt);
            sb.AppendLine();
            sb.AppendLine("--- HISTORY---");
            foreach (var msg in ctx.History)
            {
                sb.AppendLine($"{msg.Role}: {msg.Content}");
                if (msg.ToolCalls?.Count > 0)
                    foreach (var tc in msg.ToolCalls)
                        sb.AppendLine(tc.Name + $"({tc.Id}): " + tc.Arguments);
            }

            sb.AppendLine();
            sb.AppendLine("--- ATTACHMENT OPTIONS ---");
            foreach (var att in reply.InputAttachments)
            {
                sb.AppendLine(att.GetJsonString());
            }

            sb.AppendLine("--- EMBEDDING LOOKUP ---");
            sb.AppendLine(reply.EmbeddingLookup);
            sb.AppendLine();
            sb.AppendLine("--- CHUNKS ---");
            foreach (var chunk in reply.Chunks)
            {
                sb.AppendLine($"{chunk.Rank} {chunk.Similarity:#,##0%}");
                sb.AppendLine("---------------------------------------");
                sb.AppendLine();
                sb.AppendLine(chunk.Content);
                sb.AppendLine();
            }

            var ms = new MemoryStream();
            ms.Write(Encoding.UTF8.GetBytes(sb.ToString()));
            ms.Position = 0;
            await FollowupWithFileAsync(ms, "trace.txt");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during trace");
            await SendExceptionAsync(ex);
            throw;
        }
    }
}
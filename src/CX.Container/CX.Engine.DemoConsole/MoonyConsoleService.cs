using System.Text;
using System.Text.Json;
using CX.Engine.ChatAgents;
using CX.Engine.Common;
using CX.Engine.Common.Embeddings;
using CX.Engine.FileServices;
using CX.Engine.Importers;
using CX.Engine.Assistants;
using CX.Engine.Assistants.Channels;
using CX.Engine.ChatAgents.OpenAI;
using CX.Engine.QAndA;
using CX.Engine.QAndA.Auto;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MoonSharp.Interpreter;
using MoonSharp.Interpreter.Interop.RegistrationPolicies;

namespace CX.Engine.DemoConsole;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class MoonyConsoleService : IHostedService, IDisposable, ILuaCoreLibrary
{
    public LuaInstance Instance;
    private SemaphoreSlim _consoleSlimLock = new(1, 1);
    public SemaphoreSlim ConsoleSlimlock => Context.Value?.ConsoleSlimLock ?? _consoleSlimLock;

    private readonly IHostApplicationLifetime _host;
    private readonly IServiceProvider _sp;
    private readonly ILogger<MoonyConsoleService> _logger;
    private readonly EmbeddingCache _embeddingCache;
    private readonly ChatCache _chatCache;
    private FileService _fileService;
    private MoonyConsoleServiceOptions _options;
    private IDisposable _optionsChangeDisposable;
    private readonly TaskCompletionSource TcsStartSignal = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource TcsStartedSignal = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource TcsStopSignal = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource TcsStoppedSignal = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private Channel _channel = null!;
    private Channel _prodChannel = null!;
    private AutoQA _autoQA = null!;
    private readonly AgentRequest _ctx;

    public readonly AsyncLocal<ContextClass> Context = new();

    private void ApplyOptions()
    {
        _channel = _sp.GetRequiredNamedService<Channel>(_options.AssistantChannelName);
        _prodChannel = _sp.GetRequiredNamedService<Channel>(_options.ProdAssistantChannelName);

        _fileService = _options.UseFileService ? _sp.GetService<FileService>() : null;

        if (!string.IsNullOrWhiteSpace(_options.AutoQA))
            _autoQA = _sp.GetRequiredNamedService<AutoQA>(_options.AutoQA);
    }

    public MoonyConsoleService(IHostApplicationLifetime host, IServiceProvider sp, ILogger<MoonyConsoleService> logger,
        IOptionsMonitor<MoonyConsoleServiceOptions> options)
    {
        _options = options?.CurrentValue ?? throw new ArgumentNullException(nameof(options));
        _options.Validate();
        _host = host ?? throw new ArgumentNullException(nameof(host));
        _sp = sp ?? throw new ArgumentNullException(nameof(sp));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _embeddingCache = sp.GetService<EmbeddingCache>();
        _chatCache = sp.GetService<ChatCache>();

        _optionsChangeDisposable = options.OnChange(newOpts =>
        {
            try
            {
                newOpts.Validate();

                if (JsonSerializer.Serialize(_options) == JsonSerializer.Serialize(newOpts))
                    return;

                _logger.LogInformation("New options received and activated.");
                _options = newOpts;
                ApplyOptions();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error validating new options: They will be ignored.");
            }
        });

        LoopAsync();
        ApplyOptions();
        _ctx = new();
        _ctx.FeedbackLock = ConsoleSlimlock;
        _ctx.UserId = "console";
        _ctx.SessionId = Guid.NewGuid().ToString();
        _ctx.UpdateHistory = true;
    }

    public void Write(string s) => Console.Write(s);

    public void WriteLine(string s = "")
    {
        var ctx = Context.Value;
        if (ctx?.Output != null)
            Context.Value?.Output?.AppendLine(s);
        else
            Console.WriteLine(s);
    }

    public void WriteLineNoWaitDefaultCtx(string s = "")
    {
        //NB: Starts a new async context
        using (ExecutionContext.SuppressFlow())
        {
            _ = Task.Run(async () =>
            {
                using var _ = await ConsoleSlimlock.UseAsync();
                WriteLine(s);
            });
        }
    }

    public void ForegroundColor(ConsoleColor color) => Console.ForegroundColor = color;
    public string ReadLine() => Console.ReadLine();
    public void ForgetHistory() => _ctx.ForgetHistory();

    public class ContextClass
    {
        public SemaphoreSlim ConsoleSlimLock;
        public StringBuilder Output;
    }

    public async Task QuizAsync()
    {
        var assistant = _channel.Assistant;

        if (assistant == null)
            throw new InvalidOperationException("No assistant set");

        SyncContext();

        if (_options.NoMemory)
            ForgetHistory();

        var qRes = await _autoQA.GenerateQuestionAsync();
        WriteLine($"< Question: {qRes.question}");

        var docName = qRes.chunk.Metadata.SourceDocument;
        if (qRes.chunk.Metadata.SourceDocumentGroup != null &&
            qRes.chunk.Metadata.SourceDocument != qRes.chunk.Metadata.SourceDocumentGroup)
            docName = docName + " attached to " + qRes.chunk.Metadata.SourceDocumentGroup;

        WriteLine(
            $"< Question from document: {docName}");

        var res = await assistant.AskAsync(qRes.question, _ctx);

        //PrintDocumentFilter(res);

        //if (!string.IsNullOrWhiteSpace(res.EmbeddingLookup))
        //    WriteLine($"< Embedding Lookup: {res.EmbeddingLookup}");
        ForegroundColor(ConsoleColor.Gray);
        WriteLine($"< Answer: {res.Answer.NullIfWhiteSpace() ?? "<No answer>"}");

        if (res.Attachments?.Count > 0)
        {
            foreach (var att in res.Attachments)
                WriteLine($"< Attachment: {att.FileName} ({att.FileUrl})");
        }

        ForegroundColor(ConsoleColor.White);

        if (_options.GenerateQuizEvals)
        {
            var evals = await _autoQA.GenerateEvalsAsync(res.Answer);
            if (string.IsNullOrWhiteSpace(evals))
                WriteLine("< Evals: None");
            else
                WriteLine($"< Evals:\r\n{evals}");
        }
    }

    public void Quit()
    {
        WriteLine("< Bye!");
        _host.StopApplication();
        TcsStopSignal.TrySetResult();
    }

    private void PrintDocumentFilter(AssistantAnswer res)
    {
        if (res.DocumentFilter != null)
            WriteLine(
                $"< Document Filter:\n  {string.Join("\n  ", res.DocumentFilter.Select(kvp => $"{kvp.Key}: {kvp.Value.FinalScore:#,##0%} ({kvp.Value.Tokens:#,##0} tokens, {kvp.Value.Chunks:#,##0} chunks)"))}");
    }

    private void SyncContext()
    {
        _ctx.Overrides.Clear();
        _ctx.Overrides.AddRange(_channel.Options.Overrides);
    }

    public async Task AskAsync(string question, bool fromPrompt = true)
    {
        var assistant = _channel.Assistant;

        if (_options.NoMemory)
            ForgetHistory();

        SyncContext();

        try
        {
            if (assistant == null)
                throw new InvalidOperationException("No assistant set");

            var questionTask = assistant.AskAsync(question, _ctx);
            if (_options.WaitForQuestionCompletion)
                await ProcessQuestionTask();
            else
                ProcessQuestionTask().FireAndForget(_logger);

            async Task ProcessQuestionTask()
            {
                var res = await questionTask;

                //ForegroundColor(ConsoleColor.Yellow);
                //PrintDocumentFilter(res);
                //if (!string.IsNullOrWhiteSpace(res.EmbeddingLookup))
                //    WriteLine($"< Embedding Lookup: {res.EmbeddingLookup}");

                using var _ = await ConsoleSlimlock.UseAsync();
                var answer = res.Answer.NullIfWhiteSpace() ?? _options.NoAnswer.NullIfWhiteSpace();
                
                if (fromPrompt)
                {
                    ForegroundColor(ConsoleColor.Gray);
                    if (answer != null)
                        WriteLine($"< {answer}");
                    ForegroundColor(ConsoleColor.White);
                }
                else
                {
                    if (answer != null)
                        WriteLine(answer);
                }

                if (res.Attachments?.Count > 0)
                {
                    foreach (var att in res.Attachments)
                        WriteLine($"< Attachment: {att.FileName} ({att.FileUrl})");
                }
            }
        }
        catch (Exception ex) when (_options.CatchAskExceptions ?? true)
        {
            using var _ = await ConsoleSlimlock.UseAsync();
            ForegroundColor(ConsoleColor.Red);
            WriteLine($"< {ex.GetType().Name}: {ex.Message}");
            ForegroundColor(ConsoleColor.White);
        }
    }

    public async Task GenerateQAAsync(int qty)
    {
        var qaFilePath = _options.QAFilePath;

        if (string.IsNullOrWhiteSpace(qaFilePath))
            throw new InvalidOperationException($"{nameof(_options.QAFilePath)} not set in configuration");

        WriteLine("< Loading QA Excel Sheet...");
        var qaDoc = new QASession();
        qaDoc.LoadFromExcel(qaFilePath);
        WriteLine($"< Adding up to {qty} new question(s) to the QA Excel Sheet...");
        var count = 0;
        var slimLock = new SemaphoreSlim(1, 1);
        await _autoQA.PopulateQADocAsync(qaDoc,
            qty,
            // ReSharper disable once AsyncVoidLambda
            async void () =>
            {
                await Task.Yield();
                using var _ = await slimLock.UseAsync();
                WriteLine($"<   {++count} questions generated");
            });
        WriteLine("< Saving QA Excel Sheet...");
        qaDoc.SaveToExcel(qaFilePath);
        WriteLine("< Done generating new QA questions.");
    }

    public async Task RunQAAsync(bool prod, int iterations)
    {
        var qaFilePath = _options.QAFilePath;
        var assistant = prod ? _prodChannel.Assistant : _channel.Assistant;

        if (assistant == null)
            throw new InvalidOperationException("No assistant set");

        SyncContext();

        if (string.IsNullOrWhiteSpace(qaFilePath))
            throw new InvalidOperationException($"{nameof(_options.QAFilePath)} not set in configuration");

        for (var i = 0; i < iterations; i++)
        {
            WriteLine($"< Running QA vs {(prod ? _options.ProdAssistantChannelName : _options.AssistantChannelName)}");
            var qaDoc = new QASession();
            if (_options is { GoogleSheetId: not null, GoogleSheetsApiKey: not null })
            {
                WriteLine("< Loading QA Google Doc...");
                await qaDoc.LoadFromGoogleSheetsAsync(_options.GoogleSheetId!, _options.GoogleSheetsApiKey!);
            }
            else
            {
                WriteLine("< Loading QA Excel Sheet...");
                qaDoc.LoadFromExcel(qaFilePath);
            }

            WriteLine("< Evaluating questions...");
            var gpt4omini = (OpenAIChatAgent)_sp.GetRequiredNamedService<IChatAgent>("OpenAI.GPT-4o-mini");
            await qaDoc.EvaluateAsync(assistant, _chatCache ?? throw new InvalidOperationException("No chat cache configured"), gpt4omini, new(),
                _fileService ?? throw new InvalidOperationException("No file service configured"), _sp);
            WriteLine("< Saving QA Excel Sheet...");
            if (iterations == 1)
                qaDoc.SaveToExcel(qaFilePath);
            else
                qaDoc.SaveToExcel($"{Path.ChangeExtension(qaFilePath, "").RemoveTrailing(".")} Iteration {i}.xlsx");
            WriteLine("< Done running QA.");
        }
    }

    public async Task UploadToProdAsync()
    {
        var uploadArchive = _options.UploadArchive;
        WriteLine($"> This will upload to {uploadArchive}. Are you sure (Y)?");
        if (ReadLine()?.ToUpper() != "Y")
        {
            WriteLine("< Aborted.");
            return;
        }

        var diskImporter = _sp.GetRequiredService<DiskImporter>();
        await diskImporter.ImportFromOptionsAsync(uploadArchive);
        WriteLine("< Done importing.");
    }

    private async Task ProcessCommandAsync(string cmd)
    {
        if (string.IsNullOrWhiteSpace(cmd))
            return;

        if (cmd.StartsWith("/"))
        {
            try
            {
                Instance.Logger = null;
                var res = await Instance.RunAsync(cmd[1..]);
                Console.WriteLine("< " + res);
            }
            catch (Exception ex) when (_options.CatchCommandExceptions ?? true)
            {
                Console.WriteLine("< " + ex.GetType().Name + ex.Message);
            }
        }
        else
            await AskAsync(cmd);
    }

    public async Task<string> RunAsync(string cmd)
    {
        WriteLineNoWaitDefaultCtx("API > " + cmd);
        var ctx = Context.Value = new()
        {
            Output = new(),
            ConsoleSlimLock = new(1, 1)
        };
        await AskAsync(cmd, fromPrompt: false);
        var output = ctx.Output.ToString().Trim();
        WriteLineNoWaitDefaultCtx("API < " + output);
        return output;
    }

    private async void LoopAsync()
    {
        try
        {
            await TcsStartSignal.Task;
            try
            {
                SaveCaches();

                UserData.RegistrationPolicy = new AutomaticRegistrationPolicy();
                Instance = _sp.GetRequiredNamedService<LuaCore>(_options.LuaCore).GetLuaInstance();
                Instance.Logger = _sp.GetLogger<MoonyConsoleService>();
                Setup(Instance);
                var shortcuts = Instance.Shortcuts;

                Console.WriteLine("Vectormind Moony Console");
                Console.WriteLine("-----------------------");
                Console.WriteLine();
                foreach (var shortcut in shortcuts)
                    Console.WriteLine("/" + shortcut.Key + ": " + shortcut.Value.description);
                Console.WriteLine();
                Console.WriteLine("Press Enter to start");
                Console.WriteLine();

                var ctx = new AgentRequest();
                ctx.UpdateHistory = true;

                TcsStartedSignal.SetResult();
            }
            catch (Exception ex)
            {
                TcsStartedSignal.SetException(ex);
                throw;
            }

            while (!TcsStopSignal.Task.IsCompleted)
            {
                if (!_options.WaitForQuestionCompletion)
                    ReadLine();

                string cmd;
                {
                    using var _ = await ConsoleSlimlock.UseAsync();
                    Write("> ");
                    cmd = ReadLine();
                }
                await ProcessCommandAsync(cmd);
            }
        }
        finally

        {
            TcsStoppedSignal.TrySetResult();
        }
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        TcsStartSignal.SetResult();
        return TcsStartedSignal.Task;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        TcsStartSignal.TrySetResult();
        TcsStopSignal.TrySetResult();
        await TcsStoppedSignal.Task;
    }

    public void SaveCaches()
    {
        try
        {
            if (_embeddingCache?.Save() ?? false)
                WriteLine($"< Saved cache with {_embeddingCache.CacheEntries} embeddings");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Saving caches");
        }

        try
        {
            if (_chatCache?.Save() ?? false)
                WriteLine($"< Saved cache with {_chatCache.CacheEntries} chat topics...");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Saving caches");
        }
    }

    public void Dispose()
    {
        _optionsChangeDisposable?.Dispose();
    }

    public void Setup(LuaInstance instance)
    {
        instance.Script.Globals["app"] = this;
        instance.Script.Globals["File"] = UserData.CreateStatic(typeof(File));
        instance.Script.Globals["Convert"] = UserData.CreateStatic(typeof(Convert));
        instance.Shortcuts["q"] = ("app.Quit()", "/q: Quits the app");
        instance.Shortcuts["forget"] = ("app.ForgetHistory()", "/forget: Forgets chat history");
        instance.Shortcuts["save"] = ("app.SaveCaches()", "/save: Save");
        instance.Shortcuts["toprod"] = ("app.UploadToProdAsync().Wait()", "/toprod: Upload vectors to production");
        instance.Shortcuts["quiz"] = ("app.QuizAsync().Wait()", "/quiz: Ask and answer a question");
        instance.Shortcuts["run_qa"] = ("app.RunQAAsync(false, 1).Wait()", "/run_qa: Run an automated QA set");
        instance.Shortcuts["run_qa_prod"] = ("app.RunQAAsync(true, 1).Wait()", "/run_qa_prod: Run an automated QA set vs Production");
        instance.Shortcuts["app.GenerateQA(1).Wait()"] = ("app.GenerateQA(1).Wait()", "/app.GenerateQA(1).Wait(): Generate an automated QA set");
    }
}
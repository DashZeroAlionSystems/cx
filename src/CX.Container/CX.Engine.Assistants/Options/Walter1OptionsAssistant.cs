using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Json;
using CX.Engine.Archives;
using CX.Engine.Archives.Pinecone;
using CX.Engine.Assistants.ArtifactAssists;
using CX.Engine.Assistants.Channels;
using CX.Engine.Assistants.Walter1;
using CX.Engine.ChatAgents;
using CX.Engine.Common;
using CX.Engine.Common.JsonSchemas;
using CX.Engine.Common.PostgreSQL;
using CX.Engine.Common.Tracing;
using CX.Engine.Importing;
using CX.Engine.TextProcessors;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using Flurl.Http;

namespace CX.Engine.Assistants.Options;

[UsedImplicitly]
public class Walter1OptionsAssistant : IAssistant, IDisposable
{
    private readonly ArtifactAssist _aa;
    private readonly ILogger<Walter1OptionsAssistant> _logger;
    private readonly IServiceProvider _sp;
    private readonly IDisposable _optionsMonitorDisposable;
    private Snapshot _snapshot;

    private class Snapshot
    {
        public Walter1OptionsAssistantOptions Options;
        public PostgreSQLClient Sql;
    }

    private void SetSnapshot(Walter1OptionsAssistantOptions opts)
    {
        var ss = new Snapshot();
        ss.Options = opts;
        ss.Sql = _sp.GetRequiredNamedService<PostgreSQLClient>(opts.PostgreSQLClientName);
        _snapshot = ss;
    }

    public Walter1OptionsAssistant([NotNull] ArtifactAssist aa, IOptionsMonitor<Walter1OptionsAssistantOptions> monitor,
        [NotNull] ILogger<Walter1OptionsAssistant> logger, IServiceProvider sp)
    {
        _aa = aa ?? throw new ArgumentNullException(nameof(aa));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _sp = sp ?? throw new ArgumentNullException(nameof(sp));
        _optionsMonitorDisposable = monitor.Snapshot(() => _snapshot?.Options, SetSnapshot, logger, sp);
    }

    [UsedImplicitly]
    public class ConversationState
    {
        public string AssistantName { get; set; }
    }

    public async Task<AssistantAnswer> AskAsync(string question, AgentRequest astCtx)
    {
        var ss = _snapshot;
        var opts = ss.Options;

        astCtx.InitConversationState<ConversationState>(new());

        var state = astCtx.GetConversationState<ConversationState>();
        try
        {
            var s = await ss.Sql.ScalarAsync<string>($"SELECT value FROM config_walter1assistants WHERE key = {state.AssistantName}");
            var assistantOpts = string.IsNullOrWhiteSpace(s) ? new() : JsonSerializer.Deserialize<Walter1AssistantOptions>(s);

            var sb = new StringBuilder();
            var req = new ArtifactAssistRequest<Walter1AssistantOptions>(assistantOpts, question)
            {
                AgentName = opts.AgentName,
                UseExecutionPlan = opts.UseExecutionPlan,
                CurrentArtifactDescriptionPrompt =
                    state.AssistantName != null
                        ? $"The active Walter-1 assistant is '{state.AssistantName}' and its current configuration is:"
                        : "No active channel",
                ChangeArtifactMethodName = "change_current_assistant_config",
                ChangeArtifactPropertyName = "new_assistant_config",
                OnArtifactChangedAsync =
                    newValue =>
                    {
                        var json = JsonSerializer.Serialize(newValue);
                        return ss.Sql.ExecuteAsync(
                            $"INSERT INTO config_walter1assistants (key, value) VALUES ({state.AssistantName}, {json}::jsonb) ON CONFLICT (key) DO UPDATE SET value = {json}::jsonb");
                    },
                OnAddedToHistory = msg =>
                {
                    if (msg.Role != "user")
                    {
                        if (sb.Length > 0)
                            sb.AppendLine("========================================");
                        sb.AppendLine(msg.Content);
                        _logger.LogInformation("========================================\r\n" + msg.Content);
                    }

                    astCtx.History.Add(msg);
                },
                OnValidate = no =>
                {
                    no.Validate();
                    if (_sp.GetNamedService<IChatAgent>(opts.AgentName) == null)
                        throw new ValidationException($"Chat agent with name {opts.AgentName} does not exist.");

                    foreach (var archive in no.EnumerateArchives())
                        if (_sp.GetNamedService<IChunkArchive>(archive) == null)
                            throw new ValidationException($"Archive with name {archive} does not exist.");

                    if (no.InputProcessors != null)
                        foreach (var textprocessor in no.InputProcessors)
                        {
                            if (_sp.GetNamedService<ITextProcessor>(textprocessor) == null)
                                throw new ValidationException($"Text processor with name {textprocessor} does not exist.");
                        }
                }
            };

            req.ChangeArtifactKeyPropertyName = "confirm_current_assistant_name";
            req.OnChangeArtifactValidateKey = key =>
            {
                var value = key.GetString();
                if (value != state.AssistantName)
                    throw new ArtifactException($"Assitant name should match the current assistant: {value} != {state.AssistantName}");
            };
            req.Prompt.Context.PineconeIds = (await ss.Sql.ListStringAsync("SELECT key FROM config_pinecones LIMIT 51")).Union(["default"]).ToList()
                .ToCappedListString(50, 1_000);
            req.Prompt.Context.PineconeNamespaceIds =
                (await ss.Sql.ListStringAsync("SELECT key FROM config_pinecone_namespaces LIMIT 51")).ToCappedListString(50, 1_000);
            req.Prompt.Context.DefaultContextualizePrompt =
                "Given a chat history and the latest user question which might reference context in the chat history, formulate a standalone question which can be understood without the chat history or any further context. Do NOT answer the question, just reformulate it if needed and otherwise return it as is. Ensure that the formulated question is in English.  Fix any spelling and grammar mistakes that you encounter.";
            req.Prompt.Context.ExampleSystemPrompt =
                "ou are a knowledge base bot for the book Hello World!  Only answer questions from extracts of the book provided to you.";
            //req.Prompt.Context.FlatQueryKeys = (await ss.Sql.ListStringAsync("SELECT key FROM config_flatqueryassistants LIMIT 51")).ToCappedListString(50, 1_000);
            req.Prompt.Instructions.Content =
                "You are an assistant designed to help the user analyze and modify configuration for Walter-1 (unstructured data) assistants in the AI platform Vectormind.";
            req.Prompt.Actions.ChangeArtifactNotes =
                "Make sure you change values using {ChangeArtifactMethodName} as action.\r\nOther actions do not change channel values.\r\nAlways send the full JSON configuration of the updated channel.";
            req.Prompt.Add("You can activate any assistant to get its configuration data (system prompt, references archives, input processors, etc).");
            req.Prompt.Add("Each assistant has a unique string identifier (name / key / id).");
            req.Prompt.Add("Assistant names have a lot of diversity - accept user input directly.  You do not know all assistant names.");
            req.Prompt.Add("Each assistant can get data from multiple archives.  Multiple assistant can access the same archive.");
            req.Prompt.Add("""
                           A Walter-1 assistant has the following configuration structure:
                           - Archive/Archives: Should have at least one archive linked via either property.  Archive: string.  Archives: string[].
                           - ChatAgent: The underlying chat agent to use.  Required.  Recommended for most use cases is 'OpenAI.GPT-4o-mini'.  For use cases that require mroe reasoning use 'OpenAI.o1-mini' 
                           - DefaultSystemPrompt: Required.
                           - DefaultContextualizePrompt: Required.
                           - CutoffContextTokens: How many tokens (max) to include from the database.  Required. Recommended 9000.
                           - MinSimilarity: The percentage similarity (0-1) required to consider a chunk from the archive for inclusion in the context.  Required.  Recommended 0.25.
                           - CutoffHistoryTokens: How many tokens (max) to include from the chat history.  Required. Recommended 9000.
                           - UseAttachments: Determines if attachments/citations from the archives are included in the response.  Default is true.  
                           """);
            req.Prompt.Add(
                "System prompt should instruct the assistant with desired behaviour and context about its function.  A good example: {ExampleSystemPrompt}");
            req.Prompt.Add("When users ask you to create an assistant also verify that its configuration aligns with their expectations.");
            req.Prompt.Add("For contextualize prompt a good reference and default value is: {DefaultContextualizePrompt}");
            req.Prompt.Add("""
                           Valid archives are:
                             - pinecone.<name>: A Pinecone database with its default namespace.  Options are: {PineconeIds}.
                             - pinecone-namespace.<name>: The key for a Pinecone namespace definition.  A pinecone namespace defition is for a namespace (with separate name from the key) in a pinecone.<name> databases.  Keys are: {PineconeNamespaceIds}.
                             - in-memory.3-large: An in-memory database which uses OpenAI's text-embedding-3-large model.
                             - in-memory.ada-002: An in-memory database which uses OpenAI's text-embedding-ada-002 model.
                           """);
            req.Prompt.Add("""
                           Valid input processors are:
                           - azure-ai-translator.en: An Azure AI Translator for English.
                           - azure-content-safety.safe: An Azure Content Moderator for safe content.
                           """);
            req.Prompt.Add("""
                           Valid chat agents are:
                           - OpenAI.gpt-4o-mini
                           - OpenAI.gpt-4o
                           - OpenAI.GPT-3.5-Turbo
                           - OpenAI.o1
                           - OpenAI.o1-mini
                           - OpenAI.o1-preview
                           """);

            [SemanticNote("Assistant names are case sensitive, check the overall list of assistants to get the correct name.")]
            async Task<Walter1AssistantOptions> ActivateAssistantConfigAsync(string newAssistantName)
            {
                var value = await ss.Sql.ScalarAsync<string>($"SELECT value FROM config_walter1assistants WHERE key = {newAssistantName}");

                if (value == null)
                    throw new ArtifactException($"Assistant does not exist: {newAssistantName}");

                var assistantOpts = JsonSerializer.Deserialize<Walter1AssistantOptions>(value);
                req.Artifact = assistantOpts;
                state.AssistantName = newAssistantName;
                return assistantOpts;
            }

            [SemanticNote("NB: use 'default' for pinecone key if not specified by the user or existing config.")]
            [SemanticNote("Max length of pinecone_namespace is 30 characters.")]
            async Task<string> CreateOrVerifyPineconeNamespaceAsync(string pinecone, string pinecone_namespace)
            {
                if (string.IsNullOrWhiteSpace(pinecone))
                    throw new ArtifactException($"{nameof(pinecone)} cannot be empty");

                if (!pinecone.StartsWith("pinecone."))
                    pinecone = "pinecone." + pinecone;

                if (_sp.GetNamedService<IChunkArchive>(pinecone) == null)
                    throw new ArtifactException($"Archive with name {pinecone} does not exist.");
                        
                if (string.IsNullOrWhiteSpace(pinecone_namespace))
                    throw new ArtifactException($"{nameof(pinecone_namespace)} cannot be empty");
                
                if (pinecone_namespace.Length > 30)
                    throw new ArtifactException($"{nameof(pinecone_namespace)} cannot be longer than 30 characters");
                
                var key = await ss.Sql.ScalarAsync<string>($"SELECT key FROM config_pinecone_namespaces WHERE value->>'PineconeArchive' = {pinecone} AND value->>'Namespace' = {pinecone_namespace}");
                
                if (key != null)
                    return $"Is present with pinecone_namespace key '{key}'";
                
                var value = JsonSerializer.Serialize(new PineconeNamespaceOptions { PineconeArchive = pinecone, Namespace = pinecone_namespace });
                key = pinecone["pinecone.".Length..] + "_" + pinecone_namespace;
                await ss.Sql.ExecuteAsync($"INSERT INTO config_pinecone_namespaces (key, value) VALUES ({key}, {value}::jsonb)");
                return $"Created with pinecone_namespace key '{key}'";
            }

            [SemanticNote("Also referred to by users as untraining a document.")]
            [SemanticNote("Local files should be prefixed with 'file://'")]
            async Task<string> RemoveDocumentFromArchiveAsync(string documentUrl)
            {
                var importer = _sp.GetRequiredService<VectorLinkImporter>();
                //if local get a filestream
                if (documentUrl.StartsWith("file://"))
                {
                    if (!_snapshot.Options.AllowLocalPaths)
                        throw new ArtifactException("Local paths are not allowed.");
                    
                    var path = documentUrl["file://".Length..];
                    if (!File.Exists(path))
                        throw new ArtifactException($"File does not exist: {path}");

                    var memoryStream = new MemoryStream();
                    using var fileStream = File.OpenRead(path);
                    await fileStream.CopyToAsync(memoryStream);
                    var docId = await memoryStream.GetSHA256GuidAsync();

                    _logger.LogInformation($"Removing document with id {docId}", documentUrl);
                    await importer.DeleteAsync(docId);

                    return "Document untrained.";
                }
                else
                {
                    //get MemoryStream from DocumentUrl using Flurl
                    _logger.LogInformation("Downloading document from {documentUrl}...", documentUrl);
                    var webStream = (await documentUrl.GetStreamAsync());
                    var memoryStream = new MemoryStream();
                    await webStream.CopyToAsync(memoryStream);
                    var docId = await memoryStream.GetSHA256GuidAsync();

                    _logger.LogInformation($"Removing document with id {docId}", documentUrl);
                    await importer.DeleteAsync(docId);

                    return "Document untrained.";
                }
            }
            
            async Task RemoveAllDocumentsFromPineconeNamespaceAsync(string name)
            {
                if (string.IsNullOrWhiteSpace(name))
                    throw new ArtifactException($"{nameof(name)} cannot be empty");
                
                if (name.StartsWith("pinecone-namespace."))
                    name = name["pinecone-namespace.".Length..];
                
                var pineconeNamespace = _sp.GetRequiredNamedService<PineconeNamespace>(name);
                await pineconeNamespace.ClearAsync();
            }

            [SemanticNote("Also referred to by users as training or importing a document.  FileName is mandatory and should include an extension.  extractImages and preferImageTextExtraction defaults to false")]
            [SemanticNote("Local files should be prefixed with 'file://'")]
            async Task<string> UploadDocumentToArchiveAsync(string documentUrl, string fileName, string archive, bool extractImages, bool preferImageTextExtraction)
            {
                try
                {
                    if (_sp.GetNamedService<IChunkArchive>(archive) == null)
                        throw new ArtifactException($"Archive with name {archive} does not exist.");
                    
                    if (string.IsNullOrWhiteSpace(fileName))
                        throw new ArtifactException($"{nameof(fileName)} cannot be empty");
                    
                    //check filename's extension
                    if (!fileName.Contains("."))
                        throw new ArtifactException($"{nameof(fileName)} should include an extension");

                    var importer = _sp.GetRequiredService<VectorLinkImporter>();
                    //get MemoryStream from DocumentUrl using Flurl
                    _logger.LogInformation("Downloading document from {documentUrl}...", documentUrl);
                    
                    //grab local filestream if path is local
                    if (documentUrl.StartsWith("file://"))
                    {
                        if (!_snapshot.Options.AllowLocalPaths)
                            throw new ArtifactException("Local paths are not allowed.");

                        var path = documentUrl["file://".Length..];
                        if (!File.Exists(path))
                            throw new ArtifactException($"File does not exist: {path}");

                        var memoryStream = new MemoryStream();
                        using var fileStream = File.OpenRead(path);
                        await fileStream.CopyToAsync(memoryStream);
                        var docId = await memoryStream.GetSHA256GuidAsync();

                        _logger.LogInformation($"Training with document id {docId}", documentUrl);
                        await importer.ImportAsync(new()
                        {
                            DocumentContent = memoryStream,
                            DocumentId = docId,
                            Archive = archive,
                            ExtractImages = extractImages,
                            PreferImageTextExtraction = preferImageTextExtraction,
                            SourceDocumentDisplayName = fileName
                        });

                        return "Training complete";
                    }
                    else
                    {
                        var webStream = (await documentUrl.GetStreamAsync());
                        var memoryStream = new MemoryStream();
                        await webStream.CopyToAsync(memoryStream);
                        var docId = await memoryStream.GetSHA256GuidAsync();

                        _logger.LogInformation($"Training with document id {docId}", documentUrl);
                        await importer.ImportAsync(new()
                        {
                            DocumentContent = memoryStream,
                            DocumentId = docId,
                            Archive = archive,
                            ExtractImages = extractImages,
                            PreferImageTextExtraction = preferImageTextExtraction,
                            SourceDocumentDisplayName = fileName
                        });

                        return "Training complete";
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Training document from URL");
                    throw;
                }
            }

            [SemanticNote("NB: does not set assistant configuration.")]
            async Task CreateAndActivateAssistantConfigAsync(string newAssistantName)
            {
                if (string.IsNullOrWhiteSpace(newAssistantName))
                    throw new ArtifactException($"{nameof(newAssistantName)} cannot be empty when creating a new assistant");

                _logger.LogDebug($"Creating assistant: {newAssistantName}");
                var opts = new Walter1AssistantOptions()
                    { Archive = "pinecone.default", DefaultSystemPrompt = "You are a Vectormind assistant for a knowledge base of user uploaded documents." };
                var value = JsonSerializer.Serialize(opts);
                try
                {
                    await ss.Sql.ExecuteAsync(
                        $"INSERT INTO config_walter1assistants (key, value) VALUES ({newAssistantName}, {value}::jsonb)");
                }
                catch (PostgresException ex)
                {
                    throw new ArtifactException($"Running SQL to create channel '{newAssistantName}': {ex.Message}");
                }

                state.AssistantName = newAssistantName;
                req.Artifact = opts;
            }

            async Task RemoveAssistantAsync(string assistantName)
            {
                _logger.LogDebug($"Remove assistant: {assistantName}");
                await ss.Sql.ExecuteAsync($"DELETE FROM config_channels WHERE key = {assistantName}");
                state.AssistantName = assistantName;
            }

            async Task<string> AskAssistantAsync(string assistantName, string question)
            {
                var assistant = _sp.GetNamedService<Walter1Assistant>(assistantName);
                if (assistant == null)
                    throw new ArtifactException($"Assistant with name {assistantName} does not exist.");

                var response = await assistant.AskAsync(question, new());
                return response.Answer;
            }

            [SemanticNote("Returns a list of assistant ids only")]
            async Task<string> ListAssistantsAsync()
            {
                var channels = await ss.Sql.ListStringAsync("SELECT key FROM config_walter1assistants LIMIT 51");
                return channels.ToCappedListString(50, 1_000);
            }

            req.Actions.Add(ListAssistantsAsync, CreateAndActivateAssistantConfigAsync, ActivateAssistantConfigAsync, RemoveAssistantAsync, CreateOrVerifyPineconeNamespaceAsync, UploadDocumentToArchiveAsync, RemoveDocumentFromArchiveAsync, AskAssistantAsync, RemoveAllDocumentsFromPineconeNamespaceAsync);

            req.SchemaObject.Properties.Remove(nameof(ChannelOptions.Overrides));
            if (astCtx.History.Count > 1)
                req.History.AddRange(astCtx.History[1..]);
            await _aa.RequestAsync(req);

            return new(sb.ToString());
        }
        finally
        {
            astCtx.SetConversationState(state);
        }
    }

    public void Dispose()
    {
        _optionsMonitorDisposable?.Dispose();
    }
}
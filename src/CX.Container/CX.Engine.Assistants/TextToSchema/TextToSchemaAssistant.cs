using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json.Nodes;
using CX.Engine.Assistants.TextToSchema.Requests;
using CX.Engine.ChatAgents.OpenAI;
using CX.Engine.Common;
using CX.Engine.Common.Json;
using CX.Engine.Common.JsonSchemas;
using CX.Engine.Common.Tracing;
using CX.Engine.Common.Tracing.Langfuse;
using CX.Engine.DocExtractors.Text;
using JetBrains.Annotations;
using Json.More;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmartFormat;

namespace CX.Engine.Assistants.TextToSchema;

public class TextToSchemaAssistant : IDisposable, IAssistant
{
    private readonly IServiceProvider _sp;
    private readonly ILogger _logger;
    private readonly IConfiguration _config;
    private readonly LangfuseService _langfuseService;
    private readonly SnapshotOptionsMonitor<TextToSchemaOptions> _optionsMonitor;
    private readonly TaskCompletionSource _tcsStarted = new();
    private readonly string _name;

    public sealed class OptionsSnapshot : DisposeTracker
    {
        public TextToSchemaOptions Options;
        public OpenAIChatAgent Chat;
        public JsonNode Schema;
        public IDisposable SchemaMonitorDisposable;

        public override void Dispose()
        {
            base.Dispose();
            SchemaMonitorDisposable?.Dispose();
            SchemaMonitorDisposable = null;
        }
    }

    private OptionsSnapshot _snapshot;

    private void UpdateSnapshot(TextToSchemaOptions opts)
    {
        var ss = new OptionsSnapshot() { Options = opts };
        try
        {
            ss.Chat = _sp.GetRequiredNamedService<OpenAIChatAgent>(opts.OpenAIChatAgentName);
            ss.Schema = opts.ResponseSchema.GetAndMonitorOpenAISchema(_config, _logger, _optionsMonitor, ss, _sp);
            _snapshot?.Dispose();
            _snapshot = ss;
            _tcsStarted.TrySetResult();
        }
        catch (Exception ex)
        {
            ss.Dispose();
            _logger.LogError(ex, "Failed to update snapshot");
        }
    }

    public TextToSchemaAssistant(string name, IOptionsMonitor<TextToSchemaOptions> options, [System.Diagnostics.CodeAnalysis.NotNull] IServiceProvider sp, ILogger logger,
        [JetBrains.Annotations.NotNull] IConfiguration config,
        [JetBrains.Annotations.NotNull] LangfuseService langfuseService)
    {
        _name = name ?? throw new ArgumentNullException(nameof(name));
        _sp = sp ?? throw new ArgumentNullException(nameof(sp));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _langfuseService = langfuseService ?? throw new ArgumentNullException(nameof(langfuseService));
        _optionsMonitor = options.Snapshot(() => _snapshot?.Options, UpdateSnapshot, logger, sp);
    }

    public void Dispose()
    {
        _snapshot?.Dispose();
        _snapshot = null;
        _optionsMonitor?.Dispose();
    }

    public async Task<AssistantAnswer> FromTextAsync(TextToSchemaRequest req)
    {
        if (req == null)
            throw new ArgumentNullException(nameof(req));

        var ireq = new TextToSchemaInternalRequest();
        ireq.Assign(req);
        ireq.Text = req.Text;
        return await HandleAsync(ireq);
    }

    public async Task<AssistantAnswer> FromImageAsync(ImageToSchemaRequest req)
    {
        if (req == null)
            throw new ArgumentNullException(nameof(req));

        var ireq = new TextToSchemaInternalRequest();
        ireq.Assign(req);
        ireq.ImageBytes = req.Bytes;
        return await HandleAsync(ireq);
    }

    public async Task<AssistantAnswer> FromPdfAsync(PdfToSchemaRequest req)
    {
        if (req == null)
            throw new ArgumentNullException(nameof(req));

        var pdfPlumber = _sp.GetRequiredService<PDFPlumber>();
        var text = await pdfPlumber.ExtractToTextAsync(req.Stream, new ());

        var ireq = new TextToSchemaInternalRequest();
        ireq.Assign(req);
        ireq.Text = text;
        return await HandleAsync(ireq);
    }

    public async Task<AssistantAnswer> FromDocXAsync(PdfToSchemaRequest req)
    {
        if (req == null)
            throw new ArgumentNullException(nameof(req));

        var pythonDocX = _sp.GetRequiredService<PythonDocX>();
        var text = await pythonDocX.ExtractToTextAsync(req.Stream, new ());

        var ireq = new TextToSchemaInternalRequest();
        ireq.Assign(req);
        ireq.Text = text;
        return await HandleAsync(ireq);
    }

    public Task<AssistantAnswer> FromDocXAsync(byte[] bytes, Dictionary<string, string> parameters = null) => FromDocXAsync(new PdfToSchemaRequest() { Stream = new MemoryStream(bytes), Parameters = parameters});
    public Task<AssistantAnswer> FromDocXAsync(Stream stream, Dictionary<string, string> parameters = null) => FromDocXAsync(new PdfToSchemaRequest() { Stream = stream, Parameters = parameters});
    public Task<AssistantAnswer> FromPdfAsync(byte[] bytes, Dictionary<string, string> parameters = null) => FromPdfAsync(new PdfToSchemaRequest() { Stream = new MemoryStream(bytes), Parameters = parameters});
    public Task<AssistantAnswer> FromPdfAsync(Stream stream, Dictionary<string, string> parameters = null) => FromPdfAsync(new PdfToSchemaRequest() { Stream = stream, Parameters = parameters});
    public Task<AssistantAnswer> FromImageAsync(byte[] bytes, Dictionary<string, string> parameters = null) => FromImageAsync(new ImageToSchemaRequest() { Bytes = bytes, Parameters = parameters });
    public Task<AssistantAnswer> FromTextAsync(string text, Dictionary<string, string> parameters = null) => FromTextAsync(new TextToSchemaRequest() { Text = text, Parameters = parameters });

    public async Task<AssistantAnswer> AskAsync(string input, AgentRequest astCtx)
    {
        astCtx ??= new();
        var trace = GetTrace(input, astCtx);
        return await trace.ExecuteAsync(async _ =>
        {
            var ireq = new TextToSchemaInternalRequest();
            if (astCtx != null)
                ireq.Assign(astCtx);
            ireq.Text = input;
            ireq.Trace = trace;

            var maybeBase64 = input.Length % 4 == 0 && input.Length > 1000;
            if (maybeBase64)
                await trace.SpanFor("maybe-base-64", new
                {
                    Len = input.Length
                }).ExecuteAsync(span =>
                {
                    try
                    {
                        var bytes = Convert.FromBase64String(input);
                        ireq.ImageBytes = bytes;
                        ireq.Text = null;
                        span.Output = new
                        {
                            IsBase64 = true,
                            Bytes = bytes.Length
                        };
                    }
                    catch
                    {
                        span.Output = new
                        {
                            IsBase64 = false
                        };
                        //ignored, not base64
                    }

                    return Task.CompletedTask;
                });

            try
            {
                return await HandleAsync(ireq);
            }
            catch (ImageToJpgException)
            {
                return new("Failed to convert image to JPEG");
            }
        });
    }

    private CXTrace GetTrace(TextToSchemaInternalRequest req)
    {
        if (req.Trace != null)
            return req.Trace;

        var trace = req.Trace = CXTrace.Current = new CXTrace(_langfuseService, req.UserId, req.SessionId)
            .WithName((req.UserId + ": " + req.Text).Preview(50))
            .WithTags(_name, "ask", "text-to-schema");
        return trace;
    }

    private CXTrace GetTrace(string input, AgentRequest req)
    {
        var trace = CXTrace.Current = new CXTrace(_langfuseService, req.UserId, req.SessionId)
            .WithName((req.UserId + ": " + input).Preview(50))
            .WithTags(_name, "ask", "text-to-schema");
        return trace;
    }

    private async Task<AssistantAnswer> HandleAsync(TextToSchemaInternalRequest astCtx)
    {
        var input = astCtx.Text;

        return await GetTrace(astCtx).ExecuteAsync<AssistantAnswer>(async _ =>
        {
            await _tcsStarted.Task;

            var ss = _snapshot;
            string jpegBase64 = null;

            if (astCtx.ImageBytes != null)
            {
                await CXTrace.Current.SpanFor("image-ops", new { ImageScaleFactor = ss.Options.ImageScaleFactor }).ExecuteAsync(span =>
                {
                    try
                    {
                        var bytes = astCtx.ImageBytes.ConvertImageToJpegAndUpscale(ss.Options.ImageScaleFactor);
                        jpegBase64 = Convert.ToBase64String(bytes);
                        span.Output = new
                        {
                            IsImage = true,
                            Bytes = bytes.Length,
                            Base64Chars = jpegBase64.Length
                        };
                    }
                    catch
                    {
                        span.Output = new
                        {
                            IsImage = false
                        };
                    }
                    return Task.CompletedTask;
                });
                
                if (jpegBase64 == null)
                    throw new ImageToJpgException();
            }

            if (ss.Options.Questions.Count > 0)
            {
                if (ss.Options.ReturnsArray)
                {
                    var req = ss.Chat.GetRequest(jpegBase64 != null ? "" : input);
                    req.ImageUrl = jpegBase64 != null ? "data:image/jpeg;base64," + jpegBase64 : null;
                    var schema = ss.Chat.GetSchema(ss.Options.ResponseSchema.OpenAIName);
                    schema.Object = new ();
                    var objTypeDef = new SchemaObject();
                    schema.Object.Properties["entries"] = new(PrimitiveTypes.Array, "The entries that have been extracted", itemType: PrimitiveTypes.Object, obj: objTypeDef);

                    foreach (var q in ss.Options.Questions)
                        if (q.Active)
                            q.AddToJsonSchema(objTypeDef, astCtx.Parameters);

                    var sb = new StringBuilder();
                    sb.AppendLine(ss.Options.ExtractionPrompt);

                    sb.AppendLine("For each entry, answer the following questions in JSON format:");
                    foreach (var q in ss.Options.Questions)
                    {
                        if (!q.Active)
                            continue;
                        
                        sb.Append($"- {q.PropertyName}: {q.Prompt}");
                        if (q.Choices != null)
                        {
                            var choices = TextToSchemaQuestion.ExpandChoices(q.Choices, astCtx.Parameters);
                            sb.Append(" (");
                            for (var i = 0; i < choices.Count; i++)
                            {
                                if (i > 0)
                                    sb.Append(" / ");
                                sb.Append(choices[i]);
                            }
                            sb.Append(")");
                        }

                        sb.AppendLine();
                    }

                    req.SystemPrompt = sb.ToString();

                    req.SetResponseSchema(schema);
                    
                    var qRes = await ss.Chat.RequestAsync(req);
                    var json = JsonNode.Parse(qRes.Answer) as JsonObject;

                    if (json == null)
                        throw new InvalidOperationException("OpenAI response was not a JSON object.");
                    
                    var entries = json["entries"] as JsonArray;
                    
                    if (entries == null)
                        throw new InvalidOperationException("Property 'entries' not found in OpenAI response.");
                    
                    if (!ss.Options.IncludeQuestionReasoning)
                        foreach (var entry in entries)
                        {
                            foreach (var q in ss.Options.Questions)
                                if (q.Active)
                                    ((JsonObject)entry).Remove(q.PropertyName + "reasoning");
                        }

                    return new(entries.ToJsonString(new() { WriteIndented = ss.Options.WriteIndented }));
                }
                else
                {
                    var mergeLock = new object();
                    var res = new JsonObject();

                    Task ProcessQuestionAsync(int i, TextToSchemaQuestion q) =>
                        CXTrace.Current.SpanFor(q.PropertyName, new { Prompt = q.Prompt, PropertyType = q.PropertyType }).ExecuteAsync(async span =>
                        {
                            var req = ss.Chat.GetRequest(jpegBase64 != null ? "" : input);
                            req.ImageUrl = jpegBase64 != null ? "data:image/jpeg;base64," + jpegBase64 : null;
                            req.SystemPrompt = Smart.Format(q.Prompt, astCtx.Parameters);
                            req.ResponseFormat =
                                OpenAIChatAgent.WrapSchema(q.GenerateJsonSchema(astCtx.Parameters), ss.Options.ResponseSchema.OpenAIName + "-q" + i);
                            var qRes = await ss.Chat.RequestAsync(req);
                            var json = JsonNode.Parse(qRes.Answer) as JsonObject;

                            if (json == null)
                                throw new InvalidOperationException("OpenAI response was not a JSON object.");

                            var propValue = json[q.PropertyName];

                            if (propValue == null)
                                throw new InvalidOperationException($"Property '{q.PropertyName}' not found in OpenAI response.");

                            if (q.NullValues != null)
                                foreach (var nv in q.NullValues)
                                    if (JsonElementEqualityComparer.Instance.Equals(propValue.ToJsonElement(), nv))
                                    {
                                        json.Remove(q.PropertyName);
                                        break;
                                    }

                            span.Output = json;

                            lock (mergeLock)
                            {
                                if (!ss.Options.IncludeQuestionReasoning)
                                    json.Remove("reasoning");

                                res = FlatJsonMerge.Merge(res, json);
                            }
                        });

                    var tasks = new List<Task>();
                    for (var i = 0; i < ss.Options.Questions.Count; i++)
                    {
                        var q = ss.Options.Questions[i];
                        if (!q.Active)
                            continue;

                        tasks.Add(ProcessQuestionAsync(i, q));
                    }

                    await tasks;

                    return new(res.ToJsonString(new() { WriteIndented = ss.Options.WriteIndented }));
                }
            }
            else
            {
                var req = ss.Chat.GetRequest(input, systemPrompt: ss.Options.ExtractionPrompt);
                req.ResponseFormat = ss.Schema;
                var res = await ss.Chat.RequestAsync(req);
                if (ss.Options.WriteIndented)
                    return new(JsonNode.Parse(res.Answer)!.ToJsonString(new() { WriteIndented = ss.Options.WriteIndented }));

                return new(res.Answer);
            }
        });
    }
}
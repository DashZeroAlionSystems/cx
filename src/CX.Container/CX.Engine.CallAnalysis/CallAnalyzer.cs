using System.Text.Json;
using CX.Engine.ChatAgents;
using CX.Engine.CognitiveServices.Blobs;
using CX.Engine.CognitiveServices.ConversationAnalysis;
using CX.Engine.CognitiveServices.LanguageDetection;
using CX.Engine.CognitiveServices.SentimentAnalysis;
using CX.Engine.CognitiveServices.ToxicityAnalysis;
using CX.Engine.CognitiveServices.Transcriptions;
using CX.Engine.Common;
using CX.Engine.Common.JsonSchemas;
using CX.Engine.Common.Tracing;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmartFormat;

namespace CX.Engine.CallAnalysis;

public class CallAnalyzer : IDisposable
{
    private readonly string _name;
    private readonly IDisposable _optionsMonitorDisposable;
    private readonly IServiceProvider _sp;
    private SnapshotClass _shapshot;
    public SnapshotClass Snapshot => _shapshot;

    public class SnapshotClass
    {
        public CallAnalyzerOptions Options;
        public TranscriptionService TranscriptionService;
        public IChatAgent ChatAgent;
        public LanguageDetector LanguageDetector;
        public SentimentAnalyzer SentimentAnalyzer;
        public ConversationAnalyzer ConversationAnalyzer;
        public ToxicityAnalyzer ToxicityAnalyzer;
    }

    private void SetSnapshot(CallAnalyzerOptions options)
    {
        var ss = new SnapshotClass();
        ss.Options = options;
        ss.TranscriptionService = _sp.GetRequiredNamedService<TranscriptionService>(options.TranscriptionServiceName);
        ss.ChatAgent = _sp.GetRequiredNamedService<IChatAgent>(options.ChatAgentName);
        ss.LanguageDetector = _sp.GetRequiredNamedService<LanguageDetector>(options.LanguageDetectorName);
        ss.SentimentAnalyzer = _sp.GetRequiredNamedService<SentimentAnalyzer>(options.SentimentAnalyzerName);
        ss.ConversationAnalyzer = _sp.GetRequiredNamedService<ConversationAnalyzer>(options.ConversationAnalyzerName);
        ss.ToxicityAnalyzer = _sp.GetRequiredNamedService<ToxicityAnalyzer>(options.ToxicityAnalyzerName);
        _shapshot = ss;
    }

    public CallAnalyzer([NotNull] string name, IOptionsMonitor<CallAnalyzerOptions> monitor, ILogger logger, IServiceProvider sp)
    {
        _name = name ?? throw new ArgumentNullException(nameof(name));
        _sp = sp ?? throw new ArgumentNullException(nameof(sp));
        _optionsMonitorDisposable = monitor.Snapshot(() => _shapshot?.Options, SetSnapshot, logger, sp);
    }
    
    public Task<string> DetermineRoleAsync(string transcript, string speakerName, SnapshotClass ss = null) =>
        CXTrace.Current.SpanFor($"determine-role for {speakerName}", null).ExecuteAsync(async span =>
        {
            ss ??= _shapshot;
            var req = ss.ChatAgent.GetRequest(
                Smart.Format(ss.Options.RoleDetectorPrompt, new
                {
                    SpeakerName = speakerName,
                    Roles = ss.Options.SpeakerRoles,
                    Transcript = transcript
                }));
            
            var schema = ss.ChatAgent.GetSchema("SpeakerClassifier");
            
            if (ss.Options.RoleDetectorIncludeReasoning)
                schema.Object.AddProperty("Reasoning", PrimitiveTypes.String);
            
            schema.Object.AddProperty("Role", PrimitiveTypes.String, choices: ss.Options.SpeakerRoles);
            req.SetResponseSchema(schema);

            var chatRes = await ss.ChatAgent.RequestAsync(req);
            var chatResJson = JsonSerializer.Deserialize<JsonElement>(chatRes.Answer);
            var role = chatResJson.GetProperty("Role").GetString();
            span.Output = new { Role = role };
            return role;
        });

    public Task DetermineRolesAsync(Transcription transcription) =>
        CXTrace.Current.SpanFor("determine-roles", new { Transcription = transcription.GetFullText() }).ExecuteAsync(async span =>
        {
            transcription.PopulateSpeakerNames();
            var transcript = transcription.GetFullText();
            Dictionary<string, int> RoleCounts = new();
            var tasks = new List<Task>();
            foreach (var speaker in transcription.Speakers.ToArray())
            {
                async Task DetermineAsync()
                {
                    var role = await DetermineRoleAsync(transcript, speaker.Value.Name);
                    lock (RoleCounts)
                    {
                        var speakerName = role + " " + RoleCounts.Inc(role);
                        transcription.Speakers[speaker.Key] = new (speakerName, role);
                    }
                }

                tasks.Add(DetermineAsync());
            }

            span.Output = new { Roles = transcription.Speakers };
            await tasks;
        });

    public Task<CallAnalyzerResult> AnalyzeAsync(Stream stream, string fileName) =>
        CXTrace.Current.SpanFor("call-analyzer", new { Filename = fileName }).ExecuteAsync(async span =>
        {
            var ss = _shapshot;
            var res = new CallAnalyzerResult();

            res.Transcription = await ss.TranscriptionService.ProcessAsync(stream, fileName);
            var fullText = res.Transcription.GetFullText();

            var determineRolesTask = DetermineRolesAsync(res.Transcription);

            var detectedLanguageTask = ss.LanguageDetector.DetectLanguageAsync(fullText);
            var sentimentTask = ss.SentimentAnalyzer.AnalyzeSentimentAsync(fullText, true);
            var toxicitiesTask = ss.ToxicityAnalyzer.AnalyzeToxicityAsync(fullText);
            
            async Task<ConversationAnalyzerResult> GetSummaryAsync()
            {
                await determineRolesTask;
                return await ss.ConversationAnalyzer.AnalyzeConversationAsync(res.Transcription);
            }
            var summariesTask = GetSummaryAsync();

            res.DetectedLanguage = await detectedLanguageTask;
            res.Sentiment = await sentimentTask;
            res.Summary = await summariesTask;
            res.Toxicity = await toxicitiesTask;
           
            span.Output = res;
            return res;
        });

    public void Dispose()
    {
        _optionsMonitorDisposable?.Dispose();
    }
}
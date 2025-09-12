using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using CX.Engine.ChatAgents;
using CX.Engine.CognitiveServices.Transcriptions;
using CX.Engine.Common;
using CX.Engine.Common.JsonSchemas;
using CX.Engine.Common.Tracing;
using Flurl.Http;
using JetBrains.Annotations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CX.Engine.CognitiveServices.ConversationAnalysis;

public class ConversationAnalyzer : ISnapshottedOptions<ConversationAnalyzer.Snapshot, ConversationAnalyzerOptions, ConversationAnalyzer>
{
    private readonly string _name;

    public Snapshot CurrentShapshot { get; set; }
    public MonitoredOptionsSection<ConversationAnalyzerOptions> OptionsSection { get; set; }

    public class Snapshot : Snapshot<ConversationAnalyzerOptions, ConversationAnalyzer>, ISnapshotSyncInit<ConversationAnalyzerOptions>
    {
        public IChatAgent ChatAgent; 
        
        public void Init(IConfigurationSection section, ILogger logger, IServiceProvider sp)
        {
            if (!string.IsNullOrWhiteSpace(Options.ChatAgentName))
                ChatAgent = sp.GetRequiredNamedService<IChatAgent>(Options.ChatAgentName);
        }
    }

    public ConversationAnalyzer(string name, [NotNull] MonitoredOptionsSection<ConversationAnalyzerOptions> monitor, ILogger logger, IServiceProvider sp)
    {
        _name = name ?? throw new ArgumentNullException(nameof(name));
        monitor.Bind<Snapshot, ConversationAnalyzer>(this);
    }

    private class ConversationItem
    {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("text")] public string Text { get; set; }
        [JsonPropertyName("role")] public string Role { get; set; }
        [JsonPropertyName("participantId")] public string ParticipantId { get; set; }
    }

    public Task<ConversationAnalyzerResult> AnalyzeConversationAsync(Transcription transcription)
    {
        var ss = CurrentShapshot;
        var opts = ss.Options;

        if (!string.IsNullOrWhiteSpace(opts.ChatAgentName))
        {
            return CXTrace.Current.SpanFor("analyze-conversation-llm", new { Transcription = transcription }).ExecuteAsync(async span =>
            {
                var sb = new StringBuilder();
                sb.AppendLine(opts.SummaryPrompt);
                
                for (var i = 0; i < transcription.Phrases.Count; i++)
                {
                    var p = transcription.Phrases[i];
                    var speaker = transcription.GetSpeaker(p.Speaker);
                    sb.AppendLine($"#{i} {speaker?.Role ?? "Unknown role"} {speaker?.Name ?? "Unknown speaeker"}: {p.Phrase}");
                }

                var req = ss.ChatAgent.GetRequest(sb.ToString());
                var schema = ss.ChatAgent.GetSchema("conversation-analyzer");
                schema.Object.AddProperty("reasoning", PrimitiveTypes.String);
                
                foreach (var kvp in opts.OutputFields)
                    schema.Object.AddProperty(kvp.Key, kvp.Value.FieldType, choices: kvp.Value.Choices);

                req.SetResponseSchema(schema);
                var resString = await ss.ChatAgent.RequestAsync(req);
                var resJson = JsonDocument.Parse(resString.Answer);
                var res = new ConversationAnalyzerResult();
                
                foreach (var kvp in opts.OutputFields)
                    res.Aspects[kvp.Key] = resJson.RootElement.GetProperty(kvp.Key).ToPrimitive();
                
                return res;
            });
        }
        else
            return CXTrace.Current.SpanFor("analyze-conversation", new { Transcription = transcription }).ExecuteAsync(async span =>
            {
                var items = new List<ConversationItem>();
                for (var i = 0; i < transcription.Phrases.Count; i++)
                {
                    var p = transcription.Phrases[i];
                    var speaker = transcription.GetSpeaker(i);
                    items.Add(new()
                    {
                        Id = i.ToString(),
                        Role = speaker?.Role ?? "Unknown",
                        ParticipantId = speaker?.Name ?? "Unknown",
                        Text = p.Phrase
                    });
                }

                var response = await $"{opts.Endpoint}"
                    .WithHeader("Ocp-Apim-Subscription-Key", opts.ApiKey)
                    .WithHeader("Accept", "application/json")
                    .AllowAnyHttpStatus()
                    .PostAsync(new StringContent(JsonSerializer.Serialize(new
                    {
                        displayName = $"Conversation Analysis {DateTime.Now}",
                        analysisInput = new
                        {
                            conversations = new[]
                            {
                                new
                                {
                                    id = "conversation1",
                                    language = "en",
                                    modality = "transcript",
                                    conversationItems = items
                                }
                            }
                        },
                        tasks = new[]
                        {
                            new
                            {
                                taskName = "summary_1",
                                kind = "ConversationalSummarizationTask",
                                parameters = new
                                {
                                    modelVersion = "latest"
                                }
                            }
                        }
                    }), Encoding.UTF8, "application/json"));

                var content = await response.GetStringAsync();
                if (!response.ResponseMessage.IsSuccessStatusCode)
                    throw new InvalidOperationException($"Conversation analysis failed. Status: {response.StatusCode}, Error: {content}");

                var operationUrl = response.ResponseMessage.Headers.GetValues("operation-location").FirstOrDefault();
                if (string.IsNullOrEmpty(operationUrl))
                    throw new InvalidOperationException("No operation-location header in response");

                // Wait for the analysis to complete
                var maxWaits = opts.MaxWaits;
                var waitCount = 0;

                while (waitCount < maxWaits)
                {
                    await Task.Delay(opts.WaitInterval);
                    var statusContent = await operationUrl
                        .WithHeader("Ocp-Apim-Subscription-Key", opts.ApiKey)
                        .GetStringAsync();

                    var statusResult = JsonSerializer.Deserialize<JsonElement>(statusContent);

                    var status = statusResult.GetProperty("status").GetString();
                    if (status == "succeeded")
                    {
                        var output = new StringBuilder();
                        var res = new ConversationAnalyzerResult();
                        output.AppendLine("Conversation Analysis:");

                        var tasks = statusResult.GetProperty("tasks").GetProperty("items");
                        foreach (var task in tasks.EnumerateArray())
                        {
                            if (task.GetProperty("kind").GetString() == "conversationalSummarizationResults")
                            {
                                var conversations = task.GetProperty("results").GetProperty("conversations");
                                foreach (var conversation in conversations.EnumerateArray())
                                {
                                    var summaries = conversation.GetProperty("summaries");
                                    foreach (var summary in summaries.EnumerateArray())
                                    {
                                        var aspect = summary.GetProperty("aspect").GetString()!;
                                        var summaryText = summary.GetProperty("text").GetString();
                                        res.Aspects[aspect] = summaryText;
                                    }

                                    if (conversation.TryGetProperty("warnings", out var warnings))
                                    {
                                        foreach (var warn in warnings.EnumerateArray())
                                        {
                                            var code = warn.GetProperty("code").GetString();
                                            var message = warn.GetProperty("message").GetString();
                                            res.Warnings.Add(new(code, message));
                                        }
                                    }
                                }
                            }
                        }

                        span.Output = res;
                        return res;
                    }
                    else if (status == "failed")
                    {
                        throw new InvalidOperationException($"Conversation analysis failed: {statusContent}");
                    }

                    waitCount++;
                }

                throw new InvalidOperationException("Conversation analysis timed out");
            });
    }
}
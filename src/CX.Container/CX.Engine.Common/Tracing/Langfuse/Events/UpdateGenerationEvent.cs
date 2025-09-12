using System.Text.Json;
using CX.Engine.Common.Json;

namespace CX.Engine.Common.Tracing.Langfuse.Events;

public class UpdateGenerationEvent : LangfuseBaseEvent
{
    public string TraceId = null!;
    public string StatusMessage;
    public string Version;
    public object Output;
    public object Metadata;
    public string GenId = null!;
    public bool End;
    public int? PromptTokens;
    public int? CompletionTokens;
    public int? TotalTokens;
    public CXTrace.ObservationLevel Level = CXTrace.ObservationLevel.DEFAULT;

    public override void Serialize(Utf8JsonWriter jw)
    {
        jw.WriteStartObject();
        jw.WriteString("type", "generation-update");
        jw.WriteString("id", Id);
        jw.WriteString("timestamp", Timestamp.ToIso8601RoundTripString());
        jw.WritePropertyName("body");
        jw.WriteStartObject();
        jw.WriteString("traceId", TraceId);
        jw.WriteString("id", GenId);
        jw.WriteString("level", Level.ToString());

        if (StatusMessage != null)
            jw.WriteString("statusMessage", StatusMessage);

        if (Version != null)
            jw.WriteString("version", Version);

        if (End)
        {
            jw.WriteString("completionStartTime", Timestamp.ToIso8601RoundTripString());
            jw.WriteString("endTime", Timestamp.ToIso8601RoundTripString());
        }

        if (Output != null)
            jw.WriteObject("output", Output);

        if (Metadata != null)
            jw.WriteObject("metadata", Metadata);

        if (PromptTokens.HasValue || CompletionTokens.HasValue || TotalTokens.HasValue)
        {
            jw.WritePropertyName("usage");
            jw.WriteStartObject();
            if (PromptTokens.HasValue)
                jw.WriteNumber("promptTokens", PromptTokens.Value);
            if (CompletionTokens.HasValue)
                jw.WriteNumber("completionTokens", CompletionTokens.Value);
            if (TotalTokens.HasValue)
                jw.WriteNumber("totalTokens", TotalTokens.Value);
            jw.WriteEndObject();
        }

        jw.WriteEndObject();
        jw.WriteEndObject();
    }
}
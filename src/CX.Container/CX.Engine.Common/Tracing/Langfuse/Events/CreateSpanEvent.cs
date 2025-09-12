using System.Text.Json;
using CX.Engine.Common.Json;

namespace CX.Engine.Common.Tracing.Langfuse.Events;

public class CreateSpanEvent : LangfuseBaseEvent
{
    public string TraceId = null!;
    public string Name = null!;
    public string StatusMessage;
    public string ParentObservationId;
    public string Version;
    public object Input;
    public object Output;
    public object Metadata;
    public string SpanId = null!;
    public DateTime? EndTime;
    
    public enum ObservationLevel
    {
        DEBUG,
        DEFAULT,
        WARNING,
        ERROR
    };

    public static string GetNewSpanId() => "sp-" + Guid.NewGuid();
    
    public CreateSpanEvent AssignNewSpanId()
    {
        SpanId = GetNewSpanId();
        return this;
    }

    public CreateSpanEvent AssignSpanId(string spanId)
    {
        SpanId = spanId;
        return this;
    }

    public override void Serialize(Utf8JsonWriter jw)
    {
        jw.WriteStartObject();
        jw.WriteString("type", "span-create");
        jw.WriteString("id", Id);
        jw.WriteString("timestamp", Timestamp.ToIso8601RoundTripString());
        jw.WritePropertyName("body");
        jw.WriteStartObject();
        jw.WriteString("traceId", TraceId);
        jw.WriteString("name", Name ?? throw new ArgumentNullException(nameof(Name)));
        jw.WriteString("startTime", Timestamp.ToIso8601RoundTripString());
        jw.WriteString("level", ObservationLevel.DEFAULT.ToString());
        jw.WriteString("id", SpanId);

        if (StatusMessage != null)
            jw.WriteString("statusMessage", StatusMessage);

        if (ParentObservationId != null)
            jw.WriteString("parentObservationId", ParentObservationId);

        if (Version != null)
            jw.WriteString("version", Version);

        if (EndTime != null)
            jw.WriteString("endTime", EndTime?.ToIso8601RoundTripString());

        if (Input != null)
            jw.WriteObject("input", Input);
        
        if (Output != null)
            jw.WriteObject("output", Output);
        
        if (Metadata != null)
            jw.WriteObject("metadata", Metadata);

        jw.WriteEndObject();
        jw.WriteEndObject();
    }
}
using System.Text.Json;
using CX.Engine.Common.Json;

namespace CX.Engine.Common.Tracing.Langfuse.Events;

public class CreateEventEvent : LangfuseBaseEvent
{
    public string TraceId = null!;
    public string Name = null!;
    public string StatusMessage;
    public string ParentObservationId;
    public string Version;
    public string EventId = null!;
    public object Input;
    public object Output;
    public object Metadata;
    public CXTrace.ObservationLevel Level = CXTrace.ObservationLevel.DEFAULT;
   
    public static string NewEventId() => "ev-" + Guid.NewGuid();
    
    public CreateEventEvent AssignNewEventId()
    {
        EventId = NewEventId();
        Id = EventId + "-create";
        return this;
    }

    public override void Serialize(Utf8JsonWriter jw)
    {
        jw.WriteStartObject();
        jw.WriteString("type", "event-create");
        jw.WriteString("id", Id);
        jw.WriteString("timestamp", Timestamp.ToIso8601RoundTripString());
        jw.WritePropertyName("body");
        jw.WriteStartObject();
        jw.WriteString("traceId", TraceId);
        jw.WriteString("name", Name);
        jw.WriteString("startTime", Timestamp.ToIso8601RoundTripString());
        jw.WriteString("level", Level.ToString());
        jw.WriteString("id", EventId);
        
        if (StatusMessage != null)
            jw.WriteString("statusMessage", StatusMessage);
        
        if (ParentObservationId != null)
            jw.WriteString("parentObservationId", ParentObservationId);
        
        if (Version != null)
            jw.WriteString("version", Version);
        
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
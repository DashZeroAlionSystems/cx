using System.Text.Json;
using CX.Engine.Common.Json;

namespace CX.Engine.Common.Tracing.Langfuse.Events;

public class UpdateSpanEvent : LangfuseBaseEvent
{
    public string TraceId = null!;
    public string StatusMessage;
    public string Version;
    public string SpanId = null!;
    public object Input;
    public object Output;
    public object Metadata;
    public CXTrace.ObservationLevel Level = CXTrace.ObservationLevel.DEFAULT; 
    public bool End;
    

    public override void Serialize(Utf8JsonWriter jw)
    {
        jw.WriteStartObject();
        jw.WriteString("type", "span-update");
        jw.WriteString("id", Id);
        jw.WriteString("timestamp", Timestamp.ToIso8601RoundTripString());
        jw.WritePropertyName("body");
        jw.WriteStartObject();
        jw.WriteString("traceId", TraceId);
        
        jw.WriteString("level", Level.ToString());
        jw.WriteString("id", SpanId);
        
        if (StatusMessage != null)
            jw.WriteString("statusMessage", StatusMessage);
        
        if (Version != null)
            jw.WriteString("version", Version);
        
        if (End)
            jw.WriteString("endTime", Timestamp.ToIso8601RoundTripString());
        
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
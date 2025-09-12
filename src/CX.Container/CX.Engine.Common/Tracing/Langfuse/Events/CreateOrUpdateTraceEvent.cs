using System.Text.Json;
using CX.Engine.Common.Json;

namespace CX.Engine.Common.Tracing.Langfuse.Events;

public class CreateOrUpdateTraceEvent : LangfuseBaseEvent
{
    public string TraceId = null!;
    public string UserId;
    public string SessionId;
    public string[] Tags;
    public object Input;
    public object Output;
    public object Metadata;
    public string Name;
    
    public static string GetNewSessionId() => "ss-" + Guid.NewGuid();
    public static string GetNewTraceId() => "tr-" + Guid.NewGuid();
    
    public CreateOrUpdateTraceEvent AssignNewSessionId()
    {
        SessionId = GetNewSessionId();
        return this;
    }
    
    public CreateOrUpdateTraceEvent AssignNewTraceId()
    {
        TraceId = GetNewTraceId();
        return this;
    }
    
    //public CreateOrUpdateTraceEvent

    public override void Serialize(Utf8JsonWriter jw)
    {
        jw.WriteStartObject();
        jw.WriteString("type", "trace-create");
        jw.WriteString("id", Id);
        jw.WriteString("timestamp", Timestamp.ToIso8601RoundTripString());
        jw.WritePropertyName("body");
        jw.WriteStartObject();
        if (UserId != null)
            jw.WriteString("userId", UserId);
        if (SessionId != null)
            jw.WriteString("sessionId", SessionId);
        if (TraceId != null)
            jw.WriteString("id", TraceId);
        
        if (Name != null)
            jw.WriteString("name", Name);

        if (Tags != null)
        {
            jw.WritePropertyName("tags");
            jw.WriteStartArray();
            foreach (var tag in Tags)
                jw.WriteStringValue(tag);
            jw.WriteEndArray();
        }

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
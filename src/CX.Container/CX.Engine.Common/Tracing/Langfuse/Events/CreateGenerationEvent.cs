using System.Text.Json;
using CX.Engine.Common.Json;

namespace CX.Engine.Common.Tracing.Langfuse.Events;

public class CreateGenerationEvent : LangfuseBaseEvent
{
    public string TraceId = null!;
    public string Name = null!;
    public string StatusMessage;
    public string ParentObservationId;
    public string Version;
    public string Model;
    public object Input;
    public object Metadata;
    public string GenId = null!;
    public Dictionary<string, object> ModelParameters = new();

    public enum ObservationLevel
    {
        DEBUG,
        DEFAULT,
        WARNING,
        ERROR
    };
    
    public static string NewGenerationId() => "gen-" + Guid.NewGuid();
    
    public CreateGenerationEvent AssignNewGenerationId()
    {
        GenId = NewGenerationId();
        Id = GenId + "-create";
        return this;
    }

    public override void Serialize(Utf8JsonWriter jw)
    {
        jw.WriteStartObject();
        jw.WriteString("type", "generation-create");
        jw.WriteString("id", Id);
        jw.WriteString("timestamp", Timestamp.ToIso8601RoundTripString());
        jw.WritePropertyName("body");
        jw.WriteStartObject();
        jw.WriteString("traceId", TraceId);
        jw.WriteString("name", Name);
        jw.WriteString("startTime", Timestamp.ToIso8601RoundTripString());
        jw.WriteString("level", ObservationLevel.DEFAULT.ToString());
        jw.WriteString("id", GenId);

        if (StatusMessage != null)
            jw.WriteString("statusMessage", StatusMessage);

        if (ParentObservationId != null)
            jw.WriteString("parentObservationId", ParentObservationId);

        if (Version != null)
            jw.WriteString("version", Version);

        if (Input != null)
            jw.WriteObject("input", Input);

        if (Metadata != null)
            jw.WriteObject("metadata", Metadata);

        if (Model != null)
            jw.WriteString("model", Model);

        if (ModelParameters?.Count > 0)
            jw.WriteObject("modelParameters", ModelParameters);

        jw.WriteEndObject();
        jw.WriteEndObject();
    }
}
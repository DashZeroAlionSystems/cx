using System.Runtime.CompilerServices;
using System.Text.Json;
using CX.Engine.Common.Json;

namespace CX.Engine.Common.Tracing.Langfuse.Events;

public abstract class LangfuseBaseEvent : ISerializeJson
{
    public readonly TaskCompletionSource Tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public Task Task => Tcs.Task;

    public DateTime Timestamp;
    public string Id;
    
    public abstract void Serialize(Utf8JsonWriter jw);
    
    public TaskAwaiter GetAwaiter() => Task.GetAwaiter();
}
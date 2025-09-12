using System.Runtime.CompilerServices;

namespace CX.Engine.Assistants.ContextAI;

public class LogThreadToolUseRequest : ContextAIRequest
{
    public string ThreadId;
    public DateTime Timestamp;
    public string Name;
    public string Observation;
    public string UserId;
    
    public readonly TaskCompletionSource<string> Tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public Task<string> Task => Tcs.Task;
    public TaskAwaiter<string> GetAwaiter() => Task.GetAwaiter();

    public LogThreadToolUseRequest(string threadId, string name, string observation, string userId, DateTime? timestamp = null)
    {
        ThreadId = threadId;
        Name = name;
        UserId = userId;
        Observation = observation;
        Timestamp = timestamp ?? DateTime.UtcNow;
        
        if (string.IsNullOrWhiteSpace(threadId))
            throw new ArgumentNullException(nameof(threadId));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentNullException(nameof(name));

        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentNullException(nameof(userId));
    }
}
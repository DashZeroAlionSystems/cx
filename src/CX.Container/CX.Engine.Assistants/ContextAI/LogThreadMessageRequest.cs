using System.Runtime.CompilerServices;

namespace CX.Engine.Assistants.ContextAI;

public class LogThreadMessageRequest : ContextAIRequest
{
    public string ThreadId;
    public string Role;
    public string UserId;
    public string Message;
    public DateTime Timestamp;
    public readonly TaskCompletionSource<string> Tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public Task<string> Task => Tcs.Task;
    public TaskAwaiter<string> GetAwaiter() => Task.GetAwaiter();

    public LogThreadMessageRequest(string threadId, string role, string userId, string message, DateTime? timestamp = null)
    {
        ThreadId = threadId;
        Role = role;
        UserId = userId;
        Message = message;
        Timestamp = timestamp ?? DateTime.UtcNow;
        
        if (string.IsNullOrWhiteSpace(threadId))
            throw new ArgumentNullException(nameof(threadId));

        if (string.IsNullOrWhiteSpace(role))
            throw new ArgumentNullException(nameof(role));

        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentNullException(nameof(userId));

        if (string.IsNullOrWhiteSpace(message))
            throw new ArgumentNullException(nameof(message));
    }
}
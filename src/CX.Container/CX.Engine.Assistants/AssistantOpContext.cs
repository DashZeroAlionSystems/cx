using CX.Engine.Common;
using JetBrains.Annotations;

namespace CX.Engine.Assistants;

public class AssistantOpContext<TSnapshot> where TSnapshot: class
{
    private readonly Func<TSnapshot> _getSnapshot;
    public readonly AsyncLocal<AssistantOpContextInstance<TSnapshot>> Local = new();
    public AssistantOpContextInstance<TSnapshot> Instance => Local.Value ??= new();
    
    public AssistantOpContext([NotNull] Func<TSnapshot> getSnapshot)
    {
        _getSnapshot = getSnapshot ?? throw new ArgumentNullException(nameof(getSnapshot));
    }
    
    private ValueTask<SemaphoreSlimDisposable> UseFeedbackSlimLockAsync => (Local.Value?.FeedbackSlimlock?.UseAsync()).IfNull(new());
    private TSnapshot OpContextSnapshot => Local.Value?.Snapshot ?? _getSnapshot();
}
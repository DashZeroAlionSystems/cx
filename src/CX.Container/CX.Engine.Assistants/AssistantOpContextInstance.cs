namespace CX.Engine.Assistants;

public class AssistantOpContextInstance<TSnapshot> where TSnapshot: class
{
    public SemaphoreSlim FeedbackSlimlock;
    public TSnapshot Snapshot;
}
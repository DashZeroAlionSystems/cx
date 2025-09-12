namespace CX.Engine.Assistants;

public static class AssistantsSharedAsyncLocal
{
    public static readonly AsyncLocal<int> AskDepthLocal = new();

    public static int AskDepth
    {
        get => AskDepthLocal.Value;
        set => AskDepthLocal.Value = value;
    }

    public static void EnterAsk()
    {
        var ad = AskDepth++;
        
        if (ad > 10)
            throw new InvalidOperationException("Maximum ask depth exceeded.");
    }
}
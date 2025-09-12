using CX.Engine.Common.Tracing.Langfuse.Events;

namespace CX.Engine.Common.Tracing.Langfuse;

public static class LangfuseExt
{
    public static void Enqueue<T>(this T ev, LangfuseService service, DateTime? now = null) where T : LangfuseBaseEvent
    {
        if (ev != null)
            service?.Enqueue(ev, now);
    }
}
using CX.Engine.Common.Tracing.Langfuse;
using CX.Engine.Common.Tracing.Langfuse.Events;
using JetBrains.Annotations;

namespace CX.Engine.Common.Tracing;

public class TracedSpanSection
{
    private readonly LangfuseService _langfuse;
    public readonly string TraceId;
    public readonly string SpanId;
    public readonly string Name;
    public readonly string ParentObservationId;
    public object Input;
    public object Output;
    public readonly Action<TracedSpanSection> OnDone;
    public CXTrace.ObservationLevel Level = CXTrace.ObservationLevel.DEFAULT;

    public TracedSpanSection([NotNull] string traceId, [NotNull] string spanId, [NotNull] string name, string parentObservationId, LangfuseService langfuse,
        Action<TracedSpanSection> onDone = null)
    {
        TraceId = traceId ?? throw new ArgumentNullException(nameof(traceId));
        SpanId = spanId ?? throw new ArgumentNullException(nameof(spanId));
        ArgumentException.ThrowIfNullOrWhiteSpace(nameof(name));
        Name = name;
        ParentObservationId = parentObservationId;
        OnDone = onDone;
        _langfuse = langfuse;
    }

    public static object CleanOutput(object output)
    {
        if (output is Exception ex)
            return ex.GetType().Name + ": " + ex.Message;

        return output;
    }

    private void Done()
    {
        Output = CleanOutput(Output);

        OnDone?.Invoke(this);
    }

    public async Task ExecuteAsync(Func<TracedSpanSection, Task> T)
    {
        Start();
        CXTrace.ObservationId.Value = SpanId;
        try
        {
            await T(this);
        }
        catch (Exception ex)
        {
            Output = ex;
            Level = CXTrace.ObservationLevel.ERROR;
            CXTrace.Current.Event(ex);
            throw;
        }
        finally
        {
            Done();
        }
    }

    private void Start()
    {
        new CreateSpanEvent
            {
                Id = SpanId + "-create",
                TraceId = TraceId,
                Name = Name ?? throw new ArgumentNullException(nameof(Name)),
                Input = Input,
                ParentObservationId = ParentObservationId,
                SpanId = SpanId
            }
            .Enqueue(_langfuse);
    }

    public async Task<T> ExecuteAsync<T>(Func<TracedSpanSection, Task<T>> task)
    {
        Start();
        CXTrace.ObservationId.Value = SpanId;
        try
        {
            return await task(this);
        }
        catch (Exception ex)
        {
            Output = ex;
            Level = CXTrace.ObservationLevel.ERROR;
            CXTrace.Current.Event(ex);
            throw;
        }
        finally
        {
            Done();
        }
    }
}
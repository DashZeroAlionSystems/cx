using System.Security.Cryptography;

namespace CX.Engine.Common.Tracing;

public readonly struct TraceOrSpan(CXTrace Trace, TracedSpanSection Span)
{
    public Task ExecuteAsync(Func<Task> action)
    {
        if (Span != null)
            return Span.ExecuteAsync(async _ => { await action(); });
        else
            return Trace.ExecuteAsync(async _ => { await action(); });
    }

    public Task<T> ExecuteAsync<T>(Func<TraceOrSpan, Task<T>> action)
    {
        var _this = this;

        if (Span != null)
            return Span.ExecuteAsync(async _ => await action(_this));
        else
            return Trace.ExecuteAsync(async _ => await action(_this));
    }

    public TraceOrSpan WithInput(object input)
    {
        if (Span != null)
            Span.Input = input;
        else
            Trace.Input = input;

        return this;
    }

    public TraceOrSpan WithTags(params string[] tags)
    {
        if (Span == null)
            Trace.WithTags(tags);

        return this;
    }

    public object Output
    {
        get => Span != null ? Span.Output : Trace.Output;
        set
        {
            if (Span != null)
                Span.Output = value;
            else
                Trace.Output = value;
        }
    }

    public static implicit operator TraceOrSpan(CXTrace trace) => new(trace, null);
    public static implicit operator TraceOrSpan(TracedSpanSection span) => new(null, span);
}
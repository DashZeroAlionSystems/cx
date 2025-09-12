using CX.Engine.Common.Tracing.Langfuse;
using CX.Engine.Common.Tracing.Langfuse.Events;
using JetBrains.Annotations;

namespace CX.Engine.Common.Tracing;

public class CXTrace
{
    public enum ObservationLevel
    {
        DEBUG,
        DEFAULT,
        WARNING,
        ERROR
    };

    private static readonly AsyncLocal<CXTrace> _current = new();
    public static readonly AsyncLocal<string> ObservationId = new();

    public static bool InTrace => _current.Value != null;
    
    public static CXTrace CurrentOrNew(Func<CXTrace> factory)
    {
        var v = _current.Value;
        return v ?? (_current.Value = factory());
    }

    public static TraceOrSpan TraceOrSpan(Func<CXTrace> traceFactory, Func<CXTrace, TracedSpanSection> spanFactory)
    {
        var trace = _current.Value;
        if (InTrace)
        {
            var span = spanFactory(trace);
            return new(trace, span);
        }
        else
        {
            Current = trace = traceFactory();
            return new(trace, null);
        }
    }

    public static CXTrace Current
    {
        get
        {
            if (_current.Value == null)
                return _current.Value = new();

            return _current.Value;
        }
        set => _current.Value = value ?? new();
    }

    public static CXTrace GetImportTrace(LangfuseService langfuse)
    {
        return new(langfuse?.Options.TraceImports ?? false ? langfuse : null,
            "importer",
            null) { Name = "import", Tags = { "import" } };
    }

    public readonly string UserId;
    public readonly string SessionId;
    public readonly string TraceId;
    public readonly DateTime Created;

    public string Name;
    public readonly HashSet<string> Tags = new();
    public object Input;

    private readonly LangfuseService _langfuse;
    private bool _traceStarted;
    private bool _traceDone;
    public object Output;

    public const string Section_Queue = "queue";
    public const string Section_GetAccessToken = "get-access-token";
    public const string Section_GetEmbedding = "get-embedding";
    public const string Section_RunPython = "run-python";
    public const string Section_CallAPI = "call-api";
    public const string Section_Import = "import";
    public const string Section_Chunk = "chunk";
    public const string Section_PDFPlumber = "pdfplumber";
    public const string Section_AnythingToMarkdown = "MarkItDownConverter";
    public const string Section_DocXToPDF = "docxtopdf";
    public const string Section_PythonDocX = "pythondocx";
    public const string Section_MSDocAnalyzer = "msdocanalyzer";
    public const string Section_GenEmbedding = "gen-embedding";
    public const string Section_GenCompletion = "gen-completion";
    public const string Section_RetrieveMatches = "retrieve-matches";
    public const string Section_Translate = "translate";
    public const string Section_Translate_Chunks = "translate-chunks";
    public const string Section_ContentSafety = "content-safety";
    public const string Section_ExtractImages = "extract-images";
    public const string Section_DetectFileFormat = "detect-file-format";
    public const string Section_PDFToImages = "pdf-to-images";
    public const string Section_QueryDB = "query-db";
    public const string Section_AcquireDistributedLock = "acquire-distributed-lock";
    public const string Section_ReleaseDistributedLock = "release-distributed-lock";

    public static string GetNewSessionId() => CreateOrUpdateTraceEvent.GetNewSessionId();
    public static string GetNewTraceId() => CreateOrUpdateTraceEvent.GetNewTraceId();

    public CXTrace()
    {
        _langfuse = null;
        TraceId = GetNewTraceId();
        Created = DateTime.UtcNow;
    }

    public CXTrace(LangfuseService langfuse, string userId, string sessionId)
    {
        _langfuse = langfuse;
        UserId = userId;
        SessionId = sessionId;
        TraceId = GetNewTraceId();
        Created = DateTime.UtcNow;
    }

    public void Done()
    {
        if (_traceDone)
            throw new InvalidOperationException("Trace already done");
        
        Tags.Add("done");

        if (!_traceStarted)
            return;

        _traceDone = true;
        Output = TracedSpanSection.CleanOutput(Output);
        
        if (Current == this)
            Current = null;

        _langfuse?.Enqueue(new CreateOrUpdateTraceEvent
        {
            Id = TraceId + "-done",
            TraceId = TraceId,
            Output = Output,
            Tags = Tags.ToArray()
        });
    }

    public TracedSpanSection SpanFor(string name, object input = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        
        var spanId = CreateSpanEvent.GetNewSpanId();

        var res = new TracedSpanSection(TraceId, spanId, name, ObservationId.Value, _langfuse,
            res =>
            {
                new UpdateSpanEvent
                {
                    Id = spanId + "-done",
                    TraceId = TraceId,
                    SpanId = spanId,
                    Output = res.Output,
                    Level = res.Level,
                    End = true
                }.Enqueue(_langfuse);
            });
        res.Input = input;
        return res;
    }

    public void Event(Exception ex)
    {
        if (ex == null)
            return;

        Event("exception", ex.GetType().Name + " - " + ex.Message, ObservationLevel.ERROR);
    }

    public void Event(string name, string message, ObservationLevel level, object output = null)
    {
        new CreateEventEvent
            {
                TraceId = TraceId,
                ParentObservationId = ObservationId.Value,
                StatusMessage = message,
                Level = level,
                Name = name,
                Output = output
            }
            .AssignNewEventId()
            .Enqueue(_langfuse);
    }

    public TracedGenSection GenerationFor(string name, string model, Dictionary<string, object> modelParameters, object input)
    {
        var gen = new CreateGenerationEvent
            {
                TraceId = TraceId,
                Name = name,
                Input = input,
                Model = model,
                ModelParameters = modelParameters,
                ParentObservationId = ObservationId.Value
            }
            .AssignNewGenerationId();
        
        gen.Enqueue(_langfuse);

        var res = new TracedGenSection(gen.GenId,
            res =>
            {
                new UpdateGenerationEvent
                {
                    Id = gen.GenId + "-done",
                    TraceId = TraceId,
                    GenId = gen.GenId,
                    Output = res.Output,
                    CompletionTokens = res.CompletionTokens,
                    PromptTokens = res.PromptTokens,
                    TotalTokens = res.TotalTokens,
                    Metadata = new
                    {
                        CachedTokens = res.CachedTokens,
                        RawPromptTokens = res.RawPromptTokens,
                        RawTotalTokens = res.RawTotalTokens,
                        ReasoningTokens = res.ReasoningTokens,
                        AudioTokens = res.AudioTokens,
                        AcceptedPredictionTokens = res.AcceptedPredictionTokens,
                        RejectedPredictionTokens = res.RejectedPredictionTokens,
                        XRateLimitLimitRequests = res.XRateLimitLimitRequests,
                        XRateLimitLimitTokens = res.XRateLimitLimitTokens,
                        XRateLimitRemainingRequests = res.XRateLimitRemainingRequests,
                        XRateLimitRemainingTokens = res.XRateLimitRemainingTokens,
                        XRateLimitResetRequests = res.XRateLimitResetRequests,
                        XRateLimitResetTokens = res.XRateLimitResetTokens
                    },
                    Level = res.Level,
                    End = true
                }.Enqueue(_langfuse);
            });
        return res;
    }

    public async Task ExecuteAsync(Func<CXTrace, Task> T)
    {
        Start();

        try
        {
            await T(this);
        }
        catch (Exception ex)
        {
            Record(ex);
            throw;
        }
        finally
        {
            Done();
        }
    }

    public void Start()
    {
        if (_traceStarted)
            throw new InvalidOperationException("Trace already started");
        
        Current = this;
        _traceStarted = true;

        new CreateOrUpdateTraceEvent
        {
            Id = TraceId + "-start",
            TraceId = TraceId,
            UserId = UserId,
            SessionId = SessionId,
            Timestamp = Created,
            Name = Name,
            Tags = Tags.ToArray(),
            Input = Input
        }.Enqueue(_langfuse);
    }

    public void Record(Exception ex)
    {
        if (!_traceStarted)
            throw new InvalidOperationException("Trace not started");
        
        if (_traceDone)
            throw new InvalidOperationException("Trace already done");
        
        Output = ex;
        Current.Event(ex);
        Tags.Add("exception");
    }

    public async Task<T> ExecuteAsync<T>(Func<CXTrace, Task<T>> task)
    {
        Start();
        try
        {
            return await task(this);
        }
        catch (Exception ex)
        {
            Record(ex);
            throw;
        }
        finally
        {
            Done();
        }
    }

    public CXTrace WithName(string name)
    {
        Name = name;
        return this;
    }

    public CXTrace WithTags(params string[] tags)
    {
        Tags.AddRange(tags.Where(tag => !string.IsNullOrWhiteSpace(tag)).Select(tag => tag!));
        return this;
    }

    public CXTrace WithInput(object input)
    {
        Input = input;
        return this;
    }
}
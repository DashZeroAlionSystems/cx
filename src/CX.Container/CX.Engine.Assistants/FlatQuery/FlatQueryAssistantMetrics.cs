using System.Text.Json;
using CX.Engine.Common.Telemetry;

namespace CX.Engine.Assistants.FlatQuery;

public sealed class FlatQueryAssistantMetrics : MetricsContainer
{
    public FlatQueryAssistantMetrics(IServiceProvider sp, string instance) : base(sp, "FlatQueryAssistant", instance)
    {   
    }

    public IncrementalLifetimeIntMetric DedupByKey_Invocations { get; } = new();
    public IncrementalLifetimeIntMetric DedupByKey_KeysScanned { get; } = new();
    public IncrementalLifetimeIntMetric DedupByKey_DupsRemoved { get; } = new();
    public IncrementalLifetimeIntMetric AntiHallucinate_Invocations { get; } = new();
    public IncrementalLifetimeIntMetric AntiHallucinate_KeysScanned { get; } = new();
    public IncrementalLifetimeIntMetric AntiHallucinate_KeysRemoved { get; } = new();
    public IncrementalLifetimeIntMetric Question_Embeddings { get; } = new();
    public IncrementalLifetimeIntMetric Question_Embeddings_TooLate { get; } = new();
    public IncrementalLifetimeIntMetric AsksNotCompletedByException { get; } = new();
    public IncrementalLifetimeIntMetric AsksNotCompletedByTimeoutException { get; } = new();
    public IncrementalLifetimeIntMetric AsksSlow = new();

    public WindowedDurationMetric Asks { get; } = new(TimeSpan.FromSeconds(10), 6);
    
    public override string ToJson()
    {
        var asks = Asks.GetSnapshot();
        
        return JsonSerializer.Serialize(new
        {
            DedupByKey_Invocations = DedupByKey_Invocations.Value,
            DedupByKey_KeysScanned = DedupByKey_KeysScanned.Value,
            DedupByKey_DupsRemoved = DedupByKey_DupsRemoved.Value,
            AntiHallucinate_Invocations = AntiHallucinate_Invocations.Value,
            AntiHallucinate_KeysScanned = AntiHallucinate_KeysScanned.Value,
            AntiHallucinate_KeysRemoved = AntiHallucinate_KeysRemoved.Value,
            Asks_MinMs = asks.WindowedMin?.TotalMilliseconds,
            Asks_MaxMs = asks.WindowedMax?.TotalMilliseconds,
            Asks_AvgMs = asks.WindowedAverage?.TotalMilliseconds,
            Asks_Count = asks.LifetimeObservations,
            Asks_TotalMs = asks.LifetimeTotal.TotalMilliseconds,
            Question_Embeddings = Question_Embeddings.Value,
            Question_Embeddings_TooLate = Question_Embeddings_TooLate.Value,
            Asks_NotCompletedByException = AsksNotCompletedByException.Value,
            Asks_NotCompletedByTimeoutException = AsksNotCompletedByTimeoutException.Value,
            Asks_Slow = AsksSlow.Value
        });
    }

    public override string ToString() => ToJson();
}
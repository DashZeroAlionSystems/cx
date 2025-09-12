using System.Text.Json;
using CX.Engine.Common.Telemetry;

namespace CX.Engine.ChatAgents.OpenAI;

public class OpenAIMetrics : MetricsContainer
{
    public OpenAIMetrics(IServiceProvider sp, string instance) : base(sp, "OpenAIMetrics", instance)
    {   
    }
    
    public IncrementalLifetimeIntMetric Asks_ExceptionsCount { get; } = new();
    public IncrementalLifetimeIntMetric Asks_429ExceptionsCount { get; } = new();
    public IncrementalLifetimeIntMetric Asks_TimeOutExceptionsCount { get; } = new();
    public IncrementalLifetimeIntMetric Asks_TotalTokenCount{ get; } = new();

    public WindowedDurationMetric Asks { get; } = new(TimeSpan.FromSeconds(10), 6);
    
    public override string ToJson()
    {
        var asks = Asks.GetSnapshot();
        
        return JsonSerializer.Serialize(new
        {
            Asks_ExceptionsCount = Asks_ExceptionsCount.Value,
            Asks_429ExceptionsCount = Asks_429ExceptionsCount.Value,
            Asks_TotalTokenCount = Asks_TotalTokenCount.Value,
            Asks_TimeOutExceptionsCount = Asks_TimeOutExceptionsCount.Value
        });
    }

    public override string ToString() => ToJson();
}
using CX.Engine.Common.PostgreSQL;
using JetBrains.Annotations;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CX.Engine.Common.Telemetry;

public class PgTelemetryRecorder : IHostedService, IDisposable, ITelemetryRecorder
{
    private readonly CancellationTokenSource _ctsStopped = new();
    private readonly TaskCompletionSource _tcsStopped = new();
    private readonly IDisposable _optionsChangeDisposable;
    private readonly IServiceProvider _sp;
    private readonly ILogger _logger;
    private readonly HashSet<string> _initializedTypes = new();
    private PgTelemetryRecorderOptions Options;
    
    public HashSet<IMetricsContainer> CollectMetricsFrom { get; } = [];

    public PgTelemetryRecorder(IOptionsMonitor<PgTelemetryRecorderOptions> options, [NotNull] ILogger<PgTelemetryRecorder> logger, [NotNull] IServiceProvider sp)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _sp = sp ?? throw new ArgumentNullException(nameof(sp));
        _optionsChangeDisposable = options.Snapshot(() => Options, o => Options = o, logger, sp);
    }

    private async Task InitTypeAsync(PostgreSQLClient client, string type)
    {
        _initializedTypes.Add(type);
        
        await client.ExecuteAsync($"""
                                   CREATE TABLE IF NOT EXISTS telemetry_{new InjectRaw(type)} (
                                       Id BIGSERIAL PRIMARY KEY,
                                       Time TIMESTAMPTZ,
                                       Instance VARCHAR(100) NOT NULL,
                                       InstanceId UUID NOT NULL,
                                       Values JSONB NOT NULL
                                   );
                                   """);
        
        await client.ExecuteAsync($"""
                                   CREATE INDEX IF NOT EXISTS idx_instance_instanceid_time ON telemetry_{new InjectRaw(type)} (Instance, InstanceId, Time)
                                   """);

        await client.ExecuteAsync($"""
                                   CREATE INDEX IF NOT EXISTS idx_instanceid_time ON telemetry_{new InjectRaw(type)} (InstanceId, Time)
                                   """);

        await client.ExecuteAsync($"""
                                   CREATE INDEX IF NOT EXISTS idx_time ON telemetry_{new InjectRaw(type)} (Time)
                                   """);
    }

    private async void Start()
    {
        try
        {
            while (!_ctsStopped.IsCancellationRequested)
            {
                try
                {
                    if (!Options.Enabled)
                    {
                        await Task.Delay(1_000);
                        continue;
                    }

                    var client = _sp.GetRequiredNamedService<PostgreSQLClient>(Options.PostgreSQLClientName);

                    foreach (var source in CollectMetricsFrom.ToArray())
                    {
                        try
                        {
                            var type = source.Type;
                            var instance = source.Instance;
                            var instanceId = source.InstanceId;
                            var json = source.ToJson();

                            if (!_initializedTypes.Contains(type))
                                await InitTypeAsync(client, type);

                            await client.ExecuteAsync($"""
                                                       INSERT INTO telemetry_{new InjectRaw(type)} (Time, Instance, InstanceId, Values)
                                                       SELECT CURRENT_TIMESTAMP, {instance}, {instanceId}, {json}::JSONB 
                                                       """);
                            
                            if (Options.CleanupEachLoop)
                            {
                                await client.ExecuteAsync($"""
                                                           DELETE FROM telemetry_{new InjectRaw(type)} 
                                                           WHERE Time < CURRENT_TIMESTAMP - INTERVAL '{new InjectRaw($"{Options.CleanupOlderThanDays:###0}")} days'
                                                           """);
                            }


                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"Error encountered in telemetry recorder loop for type {source.Type}.  Retrying in {Options.LoopInterval:c}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error encountered in telemetry recorder loop.  Retrying in {Options.LoopInterval:c}");
                }

                await Task.Delay(Options.LoopInterval);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error encountered in telemetry recorder.  Retrying in {Options.LoopInterval:c}");
        }
        finally
        {
            _tcsStopped.TrySetResult();
        }
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        Start();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _ctsStopped.Cancel();
        return _tcsStopped.Task;
    }

    public void Dispose()
    {
        _optionsChangeDisposable?.Dispose();
    }
}
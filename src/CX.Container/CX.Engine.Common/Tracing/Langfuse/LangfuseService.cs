using System.Globalization;
using System.Text.Json;
using System.Threading.Channels;
using CX.Engine.Common.Json;
using CX.Engine.Common.Tracing.Langfuse.Events;
using Flurl.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CX.Engine.Common.Tracing.Langfuse;

public class LangfuseService : IHostedService
{
    private readonly ILogger<LangfuseService> _logger;
    public readonly LangfuseOptions Options;
    private readonly Task LoopTask;
    private readonly TaskCompletionSource _tcsStarted = new();
    private static Channel<LangfuseBaseEvent> _eventQueue = null!;
    private readonly Uri _traceIngestionUri = null!;

    private volatile bool _isStopping;
    private volatile int _errors;
    private DateTime? _lastBacklogWarning;

    public LangfuseService(IOptions<LangfuseOptions> options, ILogger<LangfuseService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        Options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        Options.Validate();

        if (!Options.Enabled)
        {
            LoopTask = Task.CompletedTask;
            return;
        }

        _traceIngestionUri = new(Options.BaseUri, "/api/public/ingestion");
        if (_eventQueue == null)
        {
            _eventQueue = Channel.CreateBounded<LangfuseBaseEvent>(new BoundedChannelOptions(100_000)
            {
                FullMode = BoundedChannelFullMode.Wait,
                AllowSynchronousContinuations = false
            });
            LoopTask = LoopTaskAsync();
        }
    }

    public void Enqueue<T>(T ev, DateTime? now = null) where T : LangfuseBaseEvent
    {
        if (!Options.Enabled)
            return;

        ev.Timestamp = now ?? DateTime.UtcNow;
        if (!_eventQueue.Writer.TryWrite(ev))
        {
            if (_lastBacklogWarning == null || (DateTime.UtcNow - _lastBacklogWarning.Value).TotalMinutes > 5)
            {
                _logger.LogWarning("Langfuse backlog is full. Dropping events.");
                _lastBacklogWarning = DateTime.UtcNow;
            }
        }
    }

    private async Task LoopTaskAsync()
    {
        await _tcsStarted.Task;
        _logger.LogInformation("Langfuse Service started.");

        async Task<int> ProcessBatchAsync(List<LangfuseBaseEvent> batch)
        {
            try
            {
                using var ms = new MemoryStream();
                using var jw = new Utf8JsonWriter(ms);
                jw.WriteStartObject();
                jw.WritePropertyName("batch");
                jw.WriteStartArray();

                var items = 0;

                for (var i = 0; i < batch.Count; i++)
                {
                    var ev = batch[i];
                    var mem = ev.GetJsonMemory();

                    //Langfuse has a 4MB API request
                    if (mem.Span.Length > (4 * 1024 - 2) * 1024)
                    {
                        items = i + 1;
                        ev.Tcs.SetException(new InvalidOperationException("Event too large for Langfuse"));
                        continue;
                    }

                    //Langfuse has a 4MB API request limit
                    //We should break items up into pieces smaller than that
                    if (jw.BytesCommitted + jw.BytesPending + mem.Span.Length > (4 * 1024 - 1) * 1024)
                        break;

                    items = i + 1;
                    jw.WriteRawValue(mem.Span, true);
                }

                jw.WriteEndArray();
                jw.WriteEndObject();

                await jw.FlushAsync();
                var bytes = await _traceIngestionUri
                    .WithBasicAuth(Options.PublicKey, Options.SecretKey)
                    .PostAsync(ms.GetHttpJsonContent())
                    .ReceiveBytes();

                var response = new LangfuseResponse(bytes);

                if (response.Errors.Count > 0)
                {
                    Interlocked.Add(ref _errors, response.Errors.Count);
                    _logger.LogError($"{response.Errors.Count:#,##0} error(s) processing Langfuse request");
                    foreach (var err in response.Errors)
                    foreach (var entry in batch)
                        if (entry.Id == err.Key)
                            entry.Tcs.TrySetException(new InvalidOperationException(
                                $"Error processing Langfuse request as part of batch: status code {err.Value.code}\n{err.Value.message}\n{err.Value.error}"));
                }

                for (var i = 0; i < items; i++)
                {
                    var entry = batch[i];
                    entry.Tcs.TrySetResult();
                }

                return items;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "During Langfuse batch processing");
                foreach (var entry in batch)
                    entry.Tcs.TrySetException(ex);
                return 0;
            }
        }

        async Task ProcessBatchesAsync()
        {
            var batchBuilder = new List<LangfuseBaseEvent>();

            async Task SendAsync()
            {
                if (!Options.Enabled)
                {
                    batchBuilder.Clear();
                    return;
                }

                while (batchBuilder.Count > 0)
                {
                    var items = await ProcessBatchAsync(batchBuilder);
                    batchBuilder.RemoveRange(0, items);
                }
            }

            while (_eventQueue.Reader.TryRead(out var ev))
            {
                batchBuilder.Add(ev);

                if (batchBuilder.Count >= 200)
                    await SendAsync();
            }

            if (batchBuilder.Count > 0)
                await SendAsync();
        }

        while (!_isStopping)
        {
            try
            {
                await ProcessBatchesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Langfuse Service");
            }

            await Task.Delay(100);
        }

        await ProcessBatchesAsync();

        _logger.LogInformation("Langfuse Service stopped.");
    }


    public Task StartAsync(CancellationToken cancellationToken)
    {
        _tcsStarted.SetResult();
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _tcsStarted.TrySetResult();
        _isStopping = true;
        await LoopTask;
    }
}
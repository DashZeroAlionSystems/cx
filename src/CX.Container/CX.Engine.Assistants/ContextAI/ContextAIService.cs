using System.Text.Json;
using System.Threading.Channels;
using CX.Engine.Common;
using Flurl.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CX.Engine.Assistants.ContextAI;

public class ContextAIService : IHostedService, IDisposable
{
    private readonly ILogger<ContextAIService> _logger;
    private ContextAIOptions _options;
    private readonly Task LoopTask;
    private readonly TaskCompletionSource _tcsStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly Channel<ContextAIRequest> _channel;

    private volatile bool _stopping;
    private DateTime? _lastDropWarning;
    private readonly IDisposable _optionsChangeDisposable;

    public ContextAIService(IOptionsMonitor<ContextAIOptions> options, ILogger<ContextAIService> logger, IServiceProvider sp)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        _optionsChangeDisposable = options.Snapshot(() => _options, o =>
            {
                _options = o;
            }, _logger, sp);
        
        _channel = Channel.CreateBounded<ContextAIRequest>(new BoundedChannelOptions(1_000)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false
        });
        LoopTask = LoopAsync();
    }

    public void EnqueueAndForget<T>(T req) where T : ContextAIRequest => Enqueue(req);
    
    public T Enqueue<T>(T req) where T : ContextAIRequest
    {
        if (!_options.Enabled)
        {
            if (req is LogThreadMessageRequest msg)
                msg.Tcs.TrySetResult(null);
            else if (req is LogThreadToolUseRequest tool)
                tool.Tcs.TrySetResult(null);
            else
                throw new NotSupportedException("Unsupported request type: " + req.GetType().Name);

            return req;
        }

        if (!_channel.Writer.TryWrite(req))
        {
            if (_lastDropWarning == null || (DateTime.UtcNow - _lastDropWarning.Value).TotalMinutes > 5)
            {
                _lastDropWarning = DateTime.UtcNow;
                _logger.LogWarning("Unable to write to ContextAI channel: full.  Dropping messages.");
            }
        }

        return req;
    }

    private async Task LoopAsync()
    {
        await _tcsStarted.Task;

        async Task ProcessAsync()
        {
            while (_channel.Reader.TryRead(out var req))
            {
                try
                {
                    switch (req)
                    {
                        case LogThreadMessageRequest msg:
                            await LogThreadMessageAsync(msg);
                            break;
                        case LogThreadToolUseRequest tool:
                            await LogThreadToolUseAsync(tool);
                            break;
                        default:
                            throw new NotSupportedException("Unsupported request type: " + req.GetType().Name);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to log message to ContextAI");
                }
            }
        }

        while (!_stopping)
        {
            await ProcessAsync();
            await Task.Delay(100);
        }

        await ProcessAsync();
    }

    private async Task LogThreadMessageAsync(LogThreadMessageRequest req)
    {
        var opts = _options;
        
        if (!opts.Enabled)
        {
            req.Tcs.TrySetResult(null);
            return;
        }
        
        try
        {
            await using var ms = new MemoryStream();
            await using var jw = new Utf8JsonWriter(ms);
            jw.WriteStartObject();
            jw.WritePropertyName("conversation");
            jw.WriteStartObject();
            jw.WriteString("id", req.ThreadId);
            jw.WritePropertyName("messages");
            jw.WriteStartArray();
            jw.WriteStartObject();
            jw.WriteString("role", req.Role);
            jw.WriteString("message", req.Message);
            jw.WriteString("event_timestamp", req.Timestamp.ToIso8601RoundTripString());
            jw.WriteEndObject();
            jw.WriteEndArray();
            jw.WritePropertyName("metadata");
            jw.WriteStartObject();
            jw.WriteString("user_id", req.UserId);
            jw.WriteEndObject();
            jw.WriteEndObject();
            if (!string.IsNullOrWhiteSpace(opts.TenantId))
                jw.WriteString("tenant_id", opts.TenantId);
            jw.WriteEndObject();
            await jw.FlushAsync();

            var res = await opts.ConversationThreadEndpointFull
                .WithOAuthBearerToken(opts.ApiKey)
                .PostAsync(ms.GetHttpJsonContent())
                .ReceiveJson<ContextAILogThreadMessageResponse>();

            if (res?.Status != "ok")
                throw new InvalidOperationException("Failed to log message to ContextAI: " + res?.Status);

            if (res.Data == null)
                throw new InvalidOperationException("Missing 'data' property in response");

            if (string.IsNullOrWhiteSpace(res.Data.Id))
                throw new InvalidOperationException("Missing 'data.id' property in response");

            if (string.IsNullOrWhiteSpace(res.Data.ProvidedId))
                throw new InvalidOperationException("Missing 'data.providedid' property in response");

            req.Tcs.TrySetResult(res.Data.Id);
        }
        catch (Exception ex)
        {
            req.Tcs.TrySetException(ex);
            throw;
        }
    }

    private async Task LogThreadToolUseAsync(LogThreadToolUseRequest req)
    {
        var opts = _options;
        
        if (!opts.Enabled)
        {
            req.Tcs.TrySetResult(null);
            return;
        }
        
        try
        {
            await using var ms = new MemoryStream();
            await using var jw = new Utf8JsonWriter(ms);
            jw.WriteStartObject();
            jw.WritePropertyName("conversation");
            jw.WriteStartObject();
            jw.WriteString("id", req.ThreadId);
            jw.WritePropertyName("messages");
            jw.WriteStartArray();
            jw.WriteStartObject();
            jw.WriteString("type", "tool");
            jw.WriteString("name", req.Name);
            jw.WriteString("observation", req.Observation);
            jw.WriteString("event_timestamp", req.Timestamp.ToIso8601RoundTripString());
            jw.WriteEndObject();
            jw.WriteEndArray();
            jw.WritePropertyName("metadata");
            jw.WriteStartObject();
            jw.WriteString("user_id", req.UserId);
            jw.WriteEndObject();
            jw.WriteEndObject();
            if (!string.IsNullOrWhiteSpace(opts.TenantId))
                jw.WriteString("tenant_id", opts.TenantId);
            jw.WriteEndObject();
            await jw.FlushAsync();

            var res = await "https://api.context.ai/api/v1/log/conversation/thread"
                .WithOAuthBearerToken(opts.ApiKey)
                .PostAsync(ms.GetHttpJsonContent())
                .ReceiveJson<ContextAILogThreadMessageResponse>();

            if (res?.Status != "ok")
                throw new InvalidOperationException("Failed to log message to ContextAI: " + res?.Status);

            if (res.Data == null)
                throw new InvalidOperationException("Missing 'data' property in response");

            if (string.IsNullOrWhiteSpace(res.Data.Id))
                throw new InvalidOperationException("Missing 'data.id' property in response");

            if (string.IsNullOrWhiteSpace(res.Data.ProvidedId))
                throw new InvalidOperationException("Missing 'data.providedid' property in response");

            req.Tcs.TrySetResult(res.Data.Id);
        }
        catch (Exception ex)
        {
            req.Tcs.TrySetException(ex);
            throw;
        }
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _tcsStarted.SetResult();
        _logger.LogInformation($"ContextAI service started in {(_options.Enabled ? "enabled" : "disabled")} mode");
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _stopping = true;
        _logger.LogInformation("ContextAI service stopped");
        await LoopTask;
    }

    public void Dispose()
    {
        _optionsChangeDisposable?.Dispose();
    }
}
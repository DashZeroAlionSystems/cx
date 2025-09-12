using System.Text;
using CX.Engine.Assistants;
using CX.Engine.Common;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CX.Engine.Discord;

public class DiscordService : IDisposable
{
    private Snapshot _snapshot;
    private readonly IDisposable _optionsMonitorDisposable;
    private readonly ILogger _logger;
    private readonly IServiceProvider _sp;
    private readonly OrderedSemaphoreSlim _setOptionsSlimLock = new(1);
    private readonly TaskCompletionSource _tcsStarted = new();

    public class Snapshot
    {
        public DiscordServiceOptions Options;
        public DiscordSocketClient Client;
        public TaskCompletionSource TcsReady = new();
    }

    public async void SetOptions(DiscordServiceOptions opts)
    {
        try
        {
            using var _ = await _setOptionsSlimLock.UseAsync();

            var ss = new Snapshot();
            ss.Options = opts;

            var config = new DiscordSocketConfig
            {
                GatewayIntents = GatewayIntents.None,
                UseInteractionSnowflakeDate = false
            };

            foreach (var channel in ss.Options.Channels.Values)
            {
                channel.Assistant = _sp.GetRequiredNamedService<IAssistant>(channel.AssistantName);
            }

            ss.Client = new(config);
            InteractionService interactionService = new(ss.Client);

            ss.Client.Log += LogAsync;
            ss.Client.Ready += async () =>
            {
                await interactionService.AddModulesAsync(assembly: typeof(DiscordService).Assembly,
                    services: _sp);
                await interactionService.RegisterCommandsGloballyAsync();
                ss.TcsReady.TrySetResult();
            };
            ss.Client.InteractionCreated += async interaction =>
            {
                var scope = _sp.CreateScope();
                var ctx = new DiscordServiceInteractionContext(_snapshot, ss.Client, interaction);
                try
                {
                    await interactionService.ExecuteCommandAsync(ctx, scope.ServiceProvider);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error executing command: {ex.Message}");
                }
            };

            await ss.Client.LoginAsync(TokenType.Bot, ss.Options.Token);
            await ss.Client.StartAsync();

            _snapshot = ss;
            _tcsStarted.TrySetResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set options.");
        }
    }

    public DiscordService(IOptionsMonitor<DiscordServiceOptions> options, ILogger logger, IServiceProvider sp)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _sp = sp ?? throw new ArgumentNullException(nameof(sp));
        _optionsMonitorDisposable = options.Snapshot(() => _snapshot?.Options, SetOptions, logger, sp);
    }

    public void Dispose()
    {
        _optionsMonitorDisposable?.Dispose();
    }

    public ulong ResolveChannelId(string channelId)
    {
        if (_snapshot.Options.Channels.TryGetValue(channelId, out var channel))
            return channel.DiscordId;
        else if (ulong.TryParse(channelId, out var chId))
            return chId;
        else
            throw new InvalidOperationException("Channel not found: " + channelId);
    }

    public Task SendAsync(string channelId, string message) => SendAsync(ResolveChannelId(channelId), message);

    public async Task SendAsync(ulong channelId, string message)
    {
        //if (!ulong.TryParse(channelId, out var chId))
        //    throw new InvalidOperationException("Invalid channel ID (not ulong): " + channelId);
        
        var ss = _snapshot;
        await ss.TcsReady;
        var channel = await ss.Client.GetChannelAsync(channelId) as ITextChannel;
        
        if (channel == null)
            throw new  InvalidOperationException("Channel not found or not a text channel: " + channelId);
        
        await channel.SendMessageAsync(message);
    }

    private async Task SendTextAsync(ulong channelId, string s)
    {
        if (string.IsNullOrWhiteSpace(s))
            return;

        var ss = _snapshot;
        await ss.TcsReady;
        var isFile = s.Length > 1_000;
        var channel = await ss.Client.GetChannelAsync(channelId) as ITextChannel;
        
        if (channel == null)
            throw new InvalidOperationException("Channel not found or not a text channel: " + channelId);
        
        if (isFile)
            await channel.SendFileAsync(new FileAttachment(new MemoryStream(Encoding.UTF8.GetBytes(s)), "response.txt"));
        else
            await channel.SendMessageAsync(s);
    }

    public Task SendFileAsync(string channelId, Stream file, string filename, string text = null) => SendFileAsync(ResolveChannelId(channelId), file, filename, text);
    public async Task SendFileAsync(ulong channelId, Stream file, string filename, string text = null)
    {
        var ss = _snapshot;
        await ss.TcsReady;
        var channel = await ss.Client.GetChannelAsync(channelId) as ITextChannel;
        
        if (channel == null)
            throw new InvalidOperationException("Channel not found or not a text channel: " + channelId);
        
        await channel.SendFileAsync(file, filename, text);
    }


    public Task SendExceptionAsync(string channelId, Exception ex) => SendExceptionAsync(ResolveChannelId(channelId), ex);
    public async Task SendExceptionAsync(ulong channelId, Exception ex)
    {
        await SendTextAsync(channelId, "Error! " + ex.GetType() + ":\r\n" + ex.Message);
    }


    private Task LogAsync(LogMessage msg)
    {
        switch (msg.Severity)
        {
            case LogSeverity.Critical:
                _logger.LogCritical(msg.Exception, msg.Message);
                break;
            case LogSeverity.Error:
                _logger.LogError(msg.Exception, msg.Message);
                break;
            case LogSeverity.Debug:
                _logger.LogDebug(msg.Exception, msg.Message);
                break;
            case LogSeverity.Info:
                _logger.LogInformation(msg.Exception, msg.Message);
                break;
            case LogSeverity.Verbose:
                _logger.LogDebug(msg.Exception, msg.Message);
                break;
            case LogSeverity.Warning:
                _logger.LogWarning(msg.Exception, msg.Message);
                break;
        }

        return Task.CompletedTask;
    }
}
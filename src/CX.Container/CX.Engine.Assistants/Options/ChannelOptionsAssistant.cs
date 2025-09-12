using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Json;
using CX.Engine.Assistants.ArtifactAssists;
using CX.Engine.Assistants.Channels;
using CX.Engine.Common;
using CX.Engine.Common.JsonSchemas;
using CX.Engine.Common.PostgreSQL;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;

namespace CX.Engine.Assistants.Options;

[UsedImplicitly]
public class ChannelOptionsAssistant : IAssistant, IDisposable
{
    private readonly ArtifactAssist _aa;
    private readonly ILogger<ChannelOptionsAssistant> _logger;
    private readonly IServiceProvider _sp;
    private readonly IDisposable _optionsMonitorDisposable;
    private Snapshot _snapshot;

    private class Snapshot
    {
        public ChannelOptionsAssistantOptions Options;
        public PostgreSQLClient Sql;
    }

    private void SetSnapshot(ChannelOptionsAssistantOptions opts)
    {
        var ss = new Snapshot();
        ss.Options = opts;
        ss.Sql = _sp.GetRequiredNamedService<PostgreSQLClient>(opts.PostgreSQLClientName);
        _snapshot = ss;
    }

    public ChannelOptionsAssistant([NotNull] ArtifactAssist aa, IOptionsMonitor<ChannelOptionsAssistantOptions> monitor,
        [NotNull] ILogger<ChannelOptionsAssistant> logger, IServiceProvider sp)
    {
        _aa = aa ?? throw new ArgumentNullException(nameof(aa));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _sp = sp ?? throw new ArgumentNullException(nameof(sp));
        _optionsMonitorDisposable = monitor.Snapshot(() => _snapshot?.Options, SetSnapshot, logger, sp);
    }

    [UsedImplicitly]
    public class ConversationState
    {
        public string ChannelName { get; set; }
    }

    public async Task<AssistantAnswer> AskAsync(string question, AgentRequest astCtx)
    {
        var ss = _snapshot;
        var opts = ss.Options;

        astCtx.InitConversationState<ConversationState>(new());

        var state = astCtx.GetConversationState<ConversationState>();
        try
        {
            var s = await ss.Sql.ScalarAsync<string>($"SELECT value FROM config_channels WHERE key = {state.ChannelName}");
            var channelOpts = string.IsNullOrWhiteSpace(s) ? new() : JsonSerializer.Deserialize<ChannelOptions>(s);

            var sb = new StringBuilder();
            var req = new ArtifactAssistRequest<ChannelOptions>(channelOpts, question)
            {
                AgentName = opts.AgentName,
                UseExecutionPlan = opts.UseExecutionPlan,
                CurrentArtifactDescriptionPrompt =
                    state.ChannelName != null ?
                    $"The active channel is '{state.ChannelName}' and its current configuration is:" :
                    "No active channel",
                ChangeArtifactMethodName = "change_current_channel_config",
                ChangeArtifactPropertyName = "new_channel_config",
                OnArtifactChangedAsync =
                    newValue =>
                    {
                        var json = JsonSerializer.Serialize(newValue);
                        return ss.Sql.ExecuteAsync(
                            $"INSERT INTO config_channels (key, value) VALUES ({state.ChannelName}, {json}::jsonb) ON CONFLICT (key) DO UPDATE SET value = {json}::jsonb");
                    },
                OnAddedToHistory = msg =>
                {
                    if (msg.Role != "user")
                    {
                        if (sb.Length > 0)
                            sb.AppendLine("========================================");
                        sb.AppendLine(msg.Content);
                    }

                    astCtx.History.Add(msg);
                },
                OnValidate = no =>
                {
                    if (_sp.GetNamedService<IAssistant>(no.AssistantName) == null)
                        throw new ValidationException($"Assistant with name {no.AssistantName} does not exist.");
                }
            };

            req.ChangeArtifactKeyPropertyName = "confirm_current_channel_name";
            req.OnChangeArtifactValidateKey = key =>
            {
                var value = key.GetString();
                if (value != state.ChannelName)
                    throw new ArtifactException($"Channel name should match the current name: {value} != {state.ChannelName}");
            };
            req.Prompt.Context.Walter1Keys = (await ss.Sql.ListStringAsync("SELECT key FROM config_walter1assistants LIMIT 51")).ToCappedListString(50, 1_000);
            req.Prompt.Context.FlatQueryKeys = (await ss.Sql.ListStringAsync("SELECT key FROM config_flatqueryassistants LIMIT 51")).ToCappedListString(50, 1_000);
            req.Prompt.Instructions.Content = "You are an assistant designed to help the user analyze and modify configuration for Channels in the AI platform Vectormind.";
            req.Prompt.Actions.ChangeArtifactNotes =
                "Make sure you change values using {ChangeArtifactMethodName} as action.\r\nOther actions do not change channel values.\r\nAlways send the full JSON configuration of the updated channel.";
            req.Prompt.Add("You can activate any channel to get its configuration data (bot, display name, show in ui status, etc).");
            req.Prompt.Add("Each channel has a unique string identifier (name / key / id).");
            req.Prompt.Add("Channel names have a lot of diversity - accept user input directly.  You do not know all channel names.");
            req.Prompt.Add("Each channel points to a named assistant.  Multiple channels can point to the same assistant.");

            req.Prompt.Add(
                """
                IAssistant named references first part (engine):
                  - walter-1 (unstructured data chatbot from one or more vector databases)
                  Available walter-1 bots (after dot) are: {Walter1Keys}
                  - flatquery (structured chatbot for a single table)
                  Available flatquery bots (after dot) are: {FlatQueryKeys}
                  - options.channel 
                """);
            
            async Task<ChannelOptions> ActivateChannelAsync(string newChannelName)
            {
                var value = await ss.Sql.ScalarAsync<string>($"SELECT value FROM config_channels WHERE key = {newChannelName}");

                if (value == null)
                    throw new ArtifactException($"Channel does not exist: {newChannelName}");

                var channelOpts = JsonSerializer.Deserialize<ChannelOptions>(value);
                req.Artifact = channelOpts;
                state.ChannelName = newChannelName;
                return channelOpts;
            }

            [SemanticNote("NB: does not set channel values.")]
            async Task CreateAndActivateChannelAsync(string newChannelName)
            {
                if (string.IsNullOrWhiteSpace(newChannelName))
                    throw new ArtifactException($"{nameof(newChannelName)} cannot be empty when creating a new channel");

                _logger.LogDebug($"Creating channel: {newChannelName}");
                var opts = new ChannelOptions() { AssistantName = "walter-1.default" };
                var value = JsonSerializer.Serialize(opts);
                try
                {
                    await ss.Sql.ExecuteAsync(
                        $"INSERT INTO config_channels (key, value) VALUES ({newChannelName}, {value}::jsonb)");
                }
                catch (PostgresException ex)
                {
                    throw new ArtifactException($"Running SQL to create channel '{newChannelName}': {ex.Message}");
                }

                state.ChannelName = newChannelName;
                req.Artifact = opts;
            }

            async Task RemoveChannelAsync(string channelName)
            {
                _logger.LogDebug($"Remove channel: {channelName}");
                await ss.Sql.ExecuteAsync($"DELETE FROM config_channels WHERE key = {channelName}");
                state.ChannelName = channelName;
            }

            [SemanticNote("Returns a list of channel ids only")]
            async Task<string> ListChannelsAsync()
            {
                var channels = await ss.Sql.ListStringAsync("SELECT key FROM config_channels LIMIT 51");
                return channels.ToCappedListString(50, 1_000);
            }

            req.Actions.Add(ListChannelsAsync, CreateAndActivateChannelAsync, ActivateChannelAsync, RemoveChannelAsync);

            req.SchemaObject.Properties.Remove(nameof(ChannelOptions.Overrides));
            if (astCtx.History.Count > 1)
                req.History.AddRange(astCtx.History[1..]);
            await _aa.RequestAsync(req);

            return new(sb.ToString());
        }
        finally
        {
            astCtx.SetConversationState(state);
        }
    }

    public void Dispose()
    {
        _optionsMonitorDisposable?.Dispose();
    }
}
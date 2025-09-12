using System.Text.Json;
using CX.Engine.Assistants.Walter1;
using CX.Engine.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CX.Engine.Assistants.Channels;

public class Channel : IDisposable
{
    private readonly ILogger _logger;
    private readonly IServiceProvider _sp;
    private readonly IDisposable _optionsChangeDisposable;

    public readonly object OptionsChangeLock = new();

    public ChannelOptions Options { get; private set; }

    private string _assistantName;
    public IAssistant Assistant { get; set; }

    private void ApplyOptions()
    {
        if (!string.IsNullOrWhiteSpace(Options.AssistantName) || string.Equals(Options.AssistantName, "python", StringComparison.InvariantCultureIgnoreCase))
        {
            if (_assistantName != Options.AssistantName)
            {
                Assistant = _sp.GetRequiredNamedService<IAssistant>(Options.AssistantName);
                _assistantName = Options.AssistantName;
            }
        }

        if (!string.IsNullOrWhiteSpace(Options.SystemPromptOverride))
        {
            if (Assistant is Walter1Assistant w1)
                w1.SystemPrompt = Options.SystemPromptOverride;
        }
    }

    public Channel(IOptionsMonitor<ChannelOptions> options, ILogger logger, IServiceProvider sp)
    {
        _sp = sp ?? throw new ArgumentNullException(nameof(sp));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        Options = options?.CurrentValue ?? throw new ArgumentNullException(nameof(options));
        ApplyOptions();
        
        _optionsChangeDisposable = options.OnChange(newOpts =>
        {
            try
            {
                if (JsonSerializer.Serialize(Options) == JsonSerializer.Serialize(newOpts))
                    return;

                _logger.LogInformation("New options received and activated.");
                lock (OptionsChangeLock)
                {
                    Options = newOpts;
                    ApplyOptions();
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error validating new options:  They will be ignored.");
            }
        });
    }

    public void Dispose()
    {
        _optionsChangeDisposable?.Dispose();
    }
}
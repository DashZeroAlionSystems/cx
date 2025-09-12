using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CX.Engine.Common.Stores.Json;

public class ConfigJsonStoreProvider : IHostedService, IDisposable
{
    public const int JsonStoreKeyLength = 100;

    private Snapshot _snapshot;
    private readonly IServiceProvider _sp;
    private readonly ILogger<ConfigJsonStoreProvider> _logger;
    private readonly List<ConfigJsonStoreSource> _sources = new();
    private readonly CancellationTokenSource _ctsStopped = new();
    private readonly IOptionsMonitor<ConfigJsonStoreProviderOptions> _optionsMonitor;
    private readonly IDisposable _optionsChangeDisposable;
    private string _lastJson;
    private readonly string _configPath;

    private readonly TaskCompletionSource _tcsLoaded = new();
    public Task Loaded => _tcsLoaded.Task;

    private TaskCompletionSource _signalRecheck = new();

    public static string ConfigPath => SecretsProvider.GetPath("dbconfig.json", false);

    private class Snapshot
    {
        public ConfigJsonStoreProviderOptions Options;
        public readonly Dictionary<string, IJsonStore> Stores = new();
    }

    private void SetSnapshot(ConfigJsonStoreProviderOptions options)
    {
        var curSnapshot = _snapshot;
        var ss = new Snapshot();
        ss.Options = options;
        _snapshot = ss;

        if (curSnapshot != null)
        {
            if (options.RefreshInterval != curSnapshot.Options.RefreshInterval)
                _signalRecheck?.TrySetResult();
        }
    }

    public ConfigJsonStoreProvider(IServiceProvider sp, IOptionsMonitor<ConfigJsonStoreProviderOptions> options,
        ILogger<ConfigJsonStoreProvider> logger)
    {
        _sp = sp ?? throw new ArgumentNullException(nameof(sp));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _optionsMonitor = options ?? throw new ArgumentNullException(nameof(options));

        _configPath = ConfigPath;
        Directory.CreateDirectory(SecretsProvider.SecretsPath);
        File.WriteAllText(_configPath, "{}");

        _optionsChangeDisposable = options.Snapshot(() => _snapshot?.Options, SetSnapshot, logger, sp);

        _sources.AddRange(sp.GetServices<ConfigJsonStoreSource>());
        StartUpdateAsync();
    }

    private async Task<JsonObject> ComputeRootAsync()
    {
        var root = new JsonObject();

        var ss = _snapshot;
        foreach (var source in _sources)
        {
            try
            {
                if (!ss.Stores.TryGetValue(source.TableName, out var store))
                    ss.Stores[source.TableName] = store = new JsonStore(source.TableName, JsonStoreKeyLength, ss.Options.PostgreSQLClientName, _sp);
                await source.MergeAsync(store, root, _logger);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error while merging source {source.GetType().Name}, some configuration has been ignored.");
            }
        }

        return root;
    }

    private async void StartUpdateAsync()
    {
        var first = true;

        while (!_ctsStopped.IsCancellationRequested)
        {
            try
            {
                var root = await ComputeRootAsync();
                JsonDuplicateKeyChecker.CheckForDuplicateKeys(root);
                var rootJson = JsonSerializer.Serialize(root, new JsonSerializerOptions { WriteIndented = true });
                var tcsChanged = new TaskCompletionSource();
                using var unused = _optionsMonitor.OnChange(_ => tcsChanged.TrySetResult());
                if (_lastJson != rootJson)
                {
                    await File.WriteAllTextAsync(_configPath, rootJson);
                    _lastJson = rootJson;
                }

                if (first)
                {
                    first = false;
                    await tcsChanged.Task;
                }
                else
                    await Task.Delay(1000);

                _tcsLoaded.TrySetResult();

                _signalRecheck = new();

                var refreshInterval = _snapshot.Options.RefreshInterval;
                if (refreshInterval > TimeSpan.Zero)
                {
                    _ = Task.Run(async () =>
                    {
                        await Task.Delay(refreshInterval);
                        _signalRecheck.TrySetResult();
                    });
                }

                await _signalRecheck.Task;
            }
            catch (Exception ex)
            {
                var retryDelay = _snapshot.Options.RetryDelay;
                if (retryDelay < TimeSpan.Zero)
                    retryDelay = TimeSpan.Zero;

                if (retryDelay > TimeSpan.Zero)
                {
                    _logger.LogError(ex, $"Error while monitoring database configuration, retrying in {retryDelay}");
                    await Task.Delay(retryDelay);
                }
                else
                {
                    _logger.LogError(ex, "Error while monitoring database configuration, retrying immediately");
                }
            }
        }
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _ctsStopped.Cancel();
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _optionsChangeDisposable?.Dispose();
    }
}
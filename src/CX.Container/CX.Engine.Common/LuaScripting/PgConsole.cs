using System.Text;
using CX.Engine.Common.DistributedLocks;
using CX.Engine.Common.PostgreSQL;
using JetBrains.Annotations;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CX.Engine.Common;

public class PgConsole : IHostedService, IDisposable
{
    private readonly ILogger _logger;
    private PgConsoleOptions _options;
    private readonly IDisposable _optionsChangeDisposable;
    private readonly CancellationTokenSource _ctsStopped = new();
    private readonly TaskCompletionSource _tcsStopped = new();
    private readonly IServiceProvider _sp;
    private readonly DistributedLockService _distributedLockService;

    private Guid? ServiceId => DistributedLockService.ServiceId;

    private async Task Init()
    {
        await _distributedLockService.ServiceIdAcquired;

        var opts = _options;
        var client = _sp.GetRequiredNamedService<PostgreSQLClient>(opts.PostgreSQLClientName);
        await client.ExecuteAsync(
            "CREATE TABLE IF NOT EXISTS pgconsole_commands (id SERIAL PRIMARY KEY, serviceid uuid, command text NOT NULL, response text, completed bool)");
    }

    private async void Start()
    {
        try
        {
            await Init();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing PgConsole.");
        }

        while (!_ctsStopped.IsCancellationRequested)
        {
            try
            {
                var opts = _options;
                var client = _sp.GetRequiredNamedService<PostgreSQLClient>(opts.PostgreSQLClientName);

                var commands = await client.ListDapperAsync<PgConsoleCommand>(
                    $"""
                     SELECT id Id, serviceid ServiceId, command Command FROM pgconsole_commands WHERE Completed IS NULL AND (ServiceId IS NULL OR ServiceId = {ServiceId})
                     """);

                async Task ExecuteCommandAsync(PgConsoleCommand command)
                {
                    try
                    {
                        var sb = new StringBuilder();
                        var core = _sp.GetRequiredNamedService<LuaCore>(opts.LuaCoreName);
                        sb.Append(await core.RunAsync(command.Command));
                        await client.ExecuteAsync($"UPDATE pgconsole_commands SET completed = true, response = {sb.ToString()} WHERE id = {command.Id}");
                    }
                    catch (Exception ex)
                    {
                        await client.ExecuteAsync($"UPDATE pgconsole_commands SET completed = false, response = {ex.ToString()} WHERE id = {command.Id}");
                    }
                }

                await (from command in commands select ExecuteCommandAsync(command));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error monitoring PgConsole table.");
            }

            await Task.Delay(1_000);
        }

        _tcsStopped.TrySetResult();
    }

    public PgConsole(IOptionsMonitor<PgConsoleOptions> options, [NotNull] ILogger<PgConsole> logger, [NotNull] IServiceProvider sp,
        [NotNull] DistributedLockService distributedLockService)
    {
        //_luaCore = luaCore;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _sp = sp ?? throw new ArgumentNullException(nameof(sp));
        _distributedLockService = distributedLockService ?? throw new ArgumentNullException(nameof(distributedLockService));
        options.CurrentValue.Validate();
        _options = options.CurrentValue;
        _optionsChangeDisposable = options.Snapshot(() => _options, o => _options = o, logger, sp);
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
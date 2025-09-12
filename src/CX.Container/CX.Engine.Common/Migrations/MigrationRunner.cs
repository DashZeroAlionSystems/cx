using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CX.Engine.Common.Migrations;

public class MigrationRunner
{
    private readonly IServiceProvider _sp;
    private readonly ILogger<MigrationRunner> _logger;

    public MigrationRunner([NotNull] IServiceProvider sp, [NotNull] ILogger<MigrationRunner> logger)
    {
        _sp = sp ?? throw new ArgumentNullException(nameof(sp));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task RunAsync()
    {
        var allMigrations = _sp.GetServices<Migration>().ToList();
        
        if (allMigrations.Count == 0)
        {
            _logger.LogInformation("No migrations to run.");
            return;
        }
        
        _logger.LogInformation("Running migrations...");
        foreach (var migration in allMigrations)
            await migration.RunAsync(_sp);
        _logger.LogInformation("Migrations complete.");
    }
}
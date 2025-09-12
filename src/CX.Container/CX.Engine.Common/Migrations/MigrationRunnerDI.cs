using CX.Engine.Common.RegistrationServices;
using Microsoft.Extensions.DependencyInjection;

namespace CX.Engine.Common.Migrations;

public static class MigrationRunnerDI
{
    public static void AddMigrationRunner(this IServiceCollection sc)
    {
        sc.AddSingleton<MigrationRunner>();
        RegistrationService.StartupTasks.Insert(0, async host =>
        {
            var runner = host.Services.GetRequiredService<MigrationRunner>();
            await runner.RunAsync();
        });
    }

    public static void AddMigration(this IServiceCollection sc, Migration migration)
    {
        sc.AddSingleton(migration);
    }
    
    public static void AddMigrations(this IServiceCollection sc, params Migration[] migrations)
    {
        foreach (var migration in migrations)
            sc.AddMigration(migration);
    }
}
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CX.Engine.Common;

public static class PgConsoleDI
{
    public const string ConfigurationSection = "PgConsole"; 
    
    public static void AddPgConsole(this IServiceCollection sc, IConfiguration config)
    {
        sc.Configure<PgConsoleOptions>(config.GetSection(nameof(PgConsole)));
        sc.AddSingleton<PgConsole>();
        sc.AddSingleton<IHostedService>(sp => sp.GetRequiredService<PgConsole>());
    }
}
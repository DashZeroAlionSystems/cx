using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CX.Engine.Common.Telemetry;

public static class PgTelemetryRecorderDI
{
    public const string ConfigurationSection = "PgTelemetryRecorder";
    
    public static void AddPgTelemetryRecorder(this IServiceCollection sc, IConfiguration configuration)
    {
        sc.Configure<PgTelemetryRecorderOptions>(configuration.GetSection(ConfigurationSection));
        sc.AddSingleton<PgTelemetryRecorder>();
        sc.AddSingleton<ITelemetryRecorder>(sp => sp.GetRequiredService<PgTelemetryRecorder>());
        sc.AddSingleton<IHostedService>(sp => sp.GetRequiredService<PgTelemetryRecorder>());
    }
}
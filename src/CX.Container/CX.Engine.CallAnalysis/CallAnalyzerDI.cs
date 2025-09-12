using CX.Engine.Common;
using CX.Engine.Common.Stores.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CX.Engine.CallAnalysis;

public static class CallAnalyzerDI
{
    public const string ConfigurationSection = "CallAnalyzers";
    public const string ConfigurationTableName = "config_call_analyzers";

    public static void AddCallAnalyzers(this IServiceCollection sc, IConfiguration configuration)
    {
        sc.AddTypedJsonConfigTable<CallAnalyzerOptions>(configuration, ConfigurationSection, ConfigurationTableName);

        sc.AddNamedSingletons<CallAnalyzer>(configuration, static (sp, config, name, optional) =>
        {
            if (optional && !config.SectionExists(ConfigurationSection, name))
                return null;

            var monitor = config.MonitorRequiredSection<CallAnalyzerOptions>(ConfigurationSection, name);
            var logger = sp.GetLogger<CallAnalyzer>(name);
            
            return new(name, monitor, logger, sp);
        });
    }
}
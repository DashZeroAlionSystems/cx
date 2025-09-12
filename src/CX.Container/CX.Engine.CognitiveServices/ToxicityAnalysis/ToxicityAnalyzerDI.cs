using CX.Engine.Common;
using CX.Engine.Common.Stores.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CX.Engine.CognitiveServices.ToxicityAnalysis;

public static class ToxicityAnalyzerDI
{
    public const string ConfigurationSection = "ToxicityAnalyzers";
    public const string ConfigurationTableName = "config_toxicity_analyzers";
    
    public static void AddToxicityAnalyzers(this IServiceCollection sc, IConfiguration configuration)
    {
        sc.AddTypedJsonConfigTable<ConfigurationSection>(configuration, ConfigurationSection, ConfigurationTableName);

        sc.AddNamedSingletons<ToxicityAnalyzer>(configuration, static (sp, config, name, optional) =>
        {
            if (optional && !config.SectionExists(ConfigurationSection, name))
                return null;

            var monitor = config.MonitorRequiredSection<ToxicityAnalyzerOptions>(ConfigurationSection, name);
            var logger = sp.GetLogger<ToxicityAnalyzer>(name);
            
            return new(name, monitor, logger, sp);
        });
    }
}
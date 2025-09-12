using CX.Engine.Common;
using CX.Engine.Common.Stores.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CX.Engine.CognitiveServices.SentimentAnalysis;

public static class SentimentAnalyzerDI
{
    public const string ConfigurationSection = "SentimentAnalyzers";
    public const string ConfigurationTableName = "config_sentiment_analyzers";
    
    public static void AddSentimentAnalyzers(this IServiceCollection sc, IConfiguration configuration)
    {
        sc.AddTypedJsonConfigTable<SentimentAnalyzerOptions>(configuration, ConfigurationSection, ConfigurationTableName);

        sc.AddNamedSingletons<SentimentAnalyzer>(configuration, static (sp, config, name, optional) =>
        {
            if (optional && !config.SectionExists(ConfigurationSection, name))
                return null;

            var monitor = config.MonitorRequiredSection<SentimentAnalyzerOptions>(ConfigurationSection, name);
            var logger = sp.GetLogger<SentimentAnalyzer>(name);
            
            return new(name, monitor, logger, sp);
        });
    }
}
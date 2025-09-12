using CX.Engine.Common;
using CX.Engine.Common.Stores.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CX.Engine.CognitiveServices.LanguageDetection;

public static class LanguageDetectorDI
{
    public const string ConfigurationSection = "LanguageDetectors";
    public const string ConfigurationTableName = "config_language_detectors";
    
    public static void AddLanguageDetectors(this IServiceCollection sc, IConfiguration configuration)
    {
        sc.AddTypedJsonConfigTable<LanguageDetectorOptions>(configuration, ConfigurationSection, ConfigurationTableName);
        sc.AddNamedSingletons<LanguageDetector>(configuration, static (sp, config, name, optional) =>
        {
            if (optional && !config.SectionExists(ConfigurationSection, name))
                return null;
            
            var monitor = config.MonitorRequiredSection<LanguageDetectorOptions>(ConfigurationSection, name);
            var logger = sp.GetLogger<LanguageDetector>(name);
            return new(name, monitor, logger, sp);
        });
    }
}
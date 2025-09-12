using CX.Engine.Common;
using CX.Engine.Common.Stores.Json;
using CX.Engine.Common.Tracing.Langfuse;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CX.Engine.Assistants.CachedAssistants;

public static class Crc32CachedAssistantDI
{
    public const string ConfigurationSection = "Crc32CachedAssistants";
    public const string ConfigurationTableName = "config_crc32_cached_assistants";

    public static void AddCrc32CachedAssistants(this IServiceCollection sc, IConfiguration configuration) 
    {
        sc.AddTypedJsonConfigTable<Crc32CachedAssistantOptions>(configuration, ConfigurationSection, ConfigurationTableName);
        sc.AddNamedTransients<Crc32CachedAssistant>(configuration, static (sp, config, name, optional) =>
        {
            if (optional && !config.SectionExists(ConfigurationSection, name))
                return null;

            var monitor = config.MonitorRequiredSection<Crc32CachedAssistantOptions>(ConfigurationSection, name);
            var logger = sp.GetLogger<Crc32CachedAssistant>(name);
            return new(name, monitor, logger, sp, sp.GetRequiredService<LangfuseService>(), sp.GetRequiredService<Crc32JsonStore>());
        });        
    }
}
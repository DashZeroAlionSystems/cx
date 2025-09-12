using CX.Engine.Common;
using CX.Engine.Common.Json;
using CX.Engine.Common.JsonSchemas;
using CX.Engine.Common.Stores.Json;
using CX.Engine.Common.Tracing.Langfuse;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CX.Engine.Assistants.PgTableEnrichment;

public static class PgTableEnrichmentAssistantDI
{
    public const string ConfigurationSection = "PgTableEnrichmentAssistants";
    public const string ConfigurationTableName = "config_pg_table_enrichment_assistants";
    
    public static void AddPgTableEnrichmentAssistant(this IServiceCollection sc, IConfiguration configuration)
    {
        sc.AddTypedJsonConfigTable<PgTableEnrichmentAssistantOptions>(configuration, ConfigurationSection, ConfigurationTableName);
        sc.AddNamedTransients<PgTableEnrichmentAssistant>(configuration, static (sp, config, name, optional) =>
        {
            var exists = config.SectionExists(ConfigurationSection, name);

            if (optional && !exists)
                return null;
            
            var monitor = config.MonitorRequiredSection(ConfigurationSection, name, JsonOptionsSetup<PgTableEnrichmentAssistantOptions>.Factory);
            var logger = sp.GetLogger<PgTableEnrichmentAssistant>(name);
            
            return new(name, monitor, logger, sp, sp.GetRequiredService<LangfuseService>());
        });
    }
}
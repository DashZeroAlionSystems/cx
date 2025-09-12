using CX.Engine.Assistants.ContextAI;
using CX.Engine.Common;
using CX.Engine.Common.Embeddings.OpenAI;
using CX.Engine.Common.PostgreSQL;
using CX.Engine.Common.Stores.Json;
using CX.Engine.Common.Tracing.Langfuse;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CX.Engine.Assistants.FlatQuery;

public static class FlatQueryAssistantDI
{
    public const string ConfigurationSection = "FlatQueryAssistants";
    public const string ConfigurationTableName = "config_flatqueryassistants";
    
    public static void AddFlatQueryAssistants(this IServiceCollection sc, IConfiguration configuration)
    {
        sc.AddTypedJsonConfigTable<FlatQueryAssistantOptions>(configuration, ConfigurationSection, ConfigurationTableName);
        sc.AddNamedTransients<FlatQueryAssistant>(configuration, static (sp, config, name, optional) =>
        {
            if (optional && !config.SectionExists(ConfigurationSection, name))
                return null;
            
            var monitor = config.MonitorRequiredSectionE(ConfigurationSection, name, section => new FlatQueryAssistantOptionsSetup(section));
            var logger = sp.GetLogger<FlatQueryAssistant>(name);
            return new(name, monitor.monitor, sp, logger, sp.GetRequiredService<LangfuseService>(), sp.GetRequiredService<OpenAIEmbedder>(), monitor.section, sp.GetRequiredService<ContextAIService>(), sp.GetRequiredService<QueryCache>());
        });
    }
}
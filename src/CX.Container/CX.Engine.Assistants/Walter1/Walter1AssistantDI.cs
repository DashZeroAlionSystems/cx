using CX.Engine.Assistants.ContextAI;
using CX.Engine.ChatAgents;
using CX.Engine.Common;
using CX.Engine.Common.Stores.Json;
using CX.Engine.Common.Tracing.Langfuse;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CX.Engine.Assistants.Walter1;

public static class Walter1AssistantDI
{
    public const string ConfigurationSection = "Walter1Assistants";
    public const string ConfigurationTableName = "config_walter1assistants";
    
    public static void AddWalter1Assistants(this IServiceCollection sc, IConfiguration configuration)
    {
        sc.AddTypedJsonConfigTable<Walter1AssistantOptions>(configuration, ConfigurationSection, ConfigurationTableName);
        
        sc.AddNamedTransients<Walter1Assistant>(configuration,
            (sp, config, name, optional) =>
            {
                if (optional && !config.SectionExists(ConfigurationSection, name))
                    return null;
                
                var opts = config.MonitorRequiredSection<Walter1AssistantOptions>(ConfigurationSection, name);
                var logger = sp.GetLogger<Walter1Assistant>(name);
                return new(name, sp.GetRequiredService<ChatCache>(),
                    opts,
                    sp,
                    sp.GetRequiredService<LangfuseService>(),
                    sp.GetRequiredService<ContextAIService>(),
                    logger);
            });
    }
}
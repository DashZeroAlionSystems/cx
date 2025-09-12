using CX.Engine.Common;
using CX.Engine.Common.Stores.Json;
using CX.Engine.Common.Tracing.Langfuse;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CX.Engine.Assistants.VectorMind;

public static class VectormindLiveAssistantsDI
{
    public const string ConfigurationSection = "VectormindLiveAssistants";
    public const string ConfigurationTableName = "config_vectormindliveassistants";

    public static void AddVectormindLiveAssistants(this IServiceCollection sc, IConfiguration configuration)
    {
        sc.AddTypedJsonConfigTable<VectormindLiveAssistantOptions>(configuration, ConfigurationSection, ConfigurationTableName);
        
        sc.AddNamedTransients<VectormindLiveAssistant>(configuration,
            (sp, config, name, optional) =>
            {
                if (optional && !config.SectionExists(ConfigurationSection, name))
                    return null;
                
                var opts = config.MonitorRequiredSection<VectormindLiveAssistantOptions>(ConfigurationSection, name);
                var logger = sp.GetLogger<VectormindLiveAssistant>(name);
                return new(opts,
                    logger,
                    sp.GetRequiredService<LangfuseService>(),
                    sp);
            });
    }
}
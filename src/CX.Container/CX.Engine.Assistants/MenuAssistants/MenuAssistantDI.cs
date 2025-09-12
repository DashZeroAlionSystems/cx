using CX.Engine.Common;
using CX.Engine.Common.Stores.Json;
using CX.Engine.Common.Tracing.Langfuse;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CX.Engine.Assistants.MenuAssistants;

public static class MenuAssistantDI
{
    public const string ConfigurationSection = "MenuAssistants";
    public const string ConfigurationTableName = "config_menu_assistants";

    public static void AddMenuAssistants(this IServiceCollection sc, IConfiguration configuration)
    {
        sc.AddTypedJsonConfigTable<MenuAssistantOptions>(configuration, ConfigurationSection, ConfigurationTableName);
        sc.AddNamedTransients<MenuAssistant>(configuration, static (sp, config, name, optional) =>
        {
            if (optional && !config.SectionExists(ConfigurationSection, name))
                return null;

            var monitor = config.MonitorRequiredSection<MenuAssistantOptions>(ConfigurationSection, name);
            var logger = sp.GetLogger<MenuAssistant>(name);
            return new(name, monitor, logger, sp, sp.GetRequiredService<LangfuseService>());
        });
    }
}
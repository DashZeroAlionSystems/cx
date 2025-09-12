using CX.Engine.Assistants.FlatQuery;
using CX.Engine.Common;
using CX.Engine.Common.Stores.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CX.Engine.Assistants.LuaAssistants;

public static class LuaAssistantDI
{
    public const string ConfigurationSection = "LuaAssistants";
    public const string ConfigurationTableName = "config_lua_assistants";
    
    public static void AddLuaAssistants(this IServiceCollection sc, IConfiguration configuration)
    {
        sc.AddTypedJsonConfigTable<LuaAssistantOptions>(configuration, ConfigurationSection, ConfigurationTableName);
        sc.AddNamedTransients<LuaAssistant>(configuration, static (sp, config, name, optional) =>
        {
            if (optional && !config.SectionExists(ConfigurationSection, name))
                return null;
            
            var monitor = config.MonitorRequiredSection<LuaAssistantOptions>(ConfigurationSection, name);
            var logger = sp.GetLogger<LuaAssistant>(name);
            return new(monitor, logger, sp);
        });
    }
}
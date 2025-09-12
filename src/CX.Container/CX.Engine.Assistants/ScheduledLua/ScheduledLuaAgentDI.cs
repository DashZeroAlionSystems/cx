using CX.Engine.Common;
using CX.Engine.Common.DistributedLocks;
using CX.Engine.Common.Stores.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CX.Engine.Assistants.ScheduledLua;

public static class ScheduledLuaAgentDI
{
    public const string ConfigurationSection = "ScheduledLuaAgents";
    public const string ConfigurationTableName = "config_scheduledluaagents";
    
    public static void AddScheduledLuaAgent(this IServiceCollection sc, IConfiguration configuration)
    {
        sc.AddTypedJsonConfigTable<ScheduledLuaAgentOptions>(configuration, ConfigurationSection, ConfigurationTableName);
        sc.AddNamedSingletons<ScheduledLuaAgent>(configuration, static (sp, config, name, optional) =>
        {
            if (optional && !config.SectionExists(ConfigurationSection, name))
                return null;
            
            var monitor = config.MonitorRequiredSectionE<ScheduledLuaAgentOptions>(ConfigurationSection, name);
            var logger = sp.GetLogger<ScheduledLuaAgent>(name);
            var distributedLockService = sp.GetRequiredService<DistributedLockService>();
            return new (sp, logger, monitor.section, monitor.monitor, distributedLockService, name);
        });
    }
}
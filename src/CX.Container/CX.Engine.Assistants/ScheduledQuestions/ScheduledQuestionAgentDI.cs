namespace CX.Engine.Assistants.ScheduledQuestions;

using Common.DistributedLocks;
using Common;
using CX.Engine.Common.Stores.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public static class ScheduledQuestionAgentDI
{
    public const string ConfigurationSection = "ScheduledQuestionAgent";
    public const string ConfigurationTableName = "config_scheduledquestionagents";
    
    public static void AddScheduledQuestionAgent(this IServiceCollection sc, IConfiguration configuration)
    {
        sc.AddTypedJsonConfigTable<ScheduledQuestionAgentOptions>(configuration, ConfigurationSection, ConfigurationTableName);
        sc.AddNamedSingletons<ScheduledQuestionAgent>(configuration, static (sp, config, name, optional) =>
        {
            if (optional && !config.SectionExists(ConfigurationSection, name))
                return null;
            
            var monitor = config.MonitorRequiredSectionE<ScheduledQuestionAgentOptions>(ConfigurationSection, name);
            var logger = sp.GetLogger<ScheduledQuestionAgent>(name);
            var distributedLockService = sp.GetRequiredService<DistributedLockService>();
            return new (sp, logger, monitor.section, monitor.monitor, distributedLockService, name);
        });
    }
}
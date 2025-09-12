using CX.Engine.Common;
using CX.Engine.Common.Stores.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CX.Engine.ChatAgents.OpenAI;

public static class OpenAIChatAgentsDI
{
    public const string ConfigurationSection  = "OpenAIChatAgents";
    public const string ConfigurationTableName = "config_openai_chat_agents";
    
    public static void AddOpenAIChatAgents(this IServiceCollection sc, IConfiguration configuration)
    {
        sc.AddTypedJsonConfigTable<OpenAIChatAgentOptions>(configuration, ConfigurationSection, ConfigurationTableName);
        
        sc.AddNamedSingletons<OpenAIChatAgent>(configuration, static (sp, config, name, optional) =>
        {
            var section = config.GetSection(ConfigurationSection, name);
            
            if (optional && !section.Exists())
                return null;
            
            var logger = sp.GetLogger<OpenAIChatAgent>(name);
            var configSection = section.GetJsonOptionsMonitor<OpenAIChatAgentOptions>(logger, sp);
            return new(configSection, logger, sp, name, sp.GetRequiredService<Crc32JsonStore>());
        });
    }
}
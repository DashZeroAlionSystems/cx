using CX.Engine.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CX.Engine.ChatAgents.Gemini;

public static class GeminiChatAgentsDI
{
    public const string ConfigurationSection  = "GeminiChatAgents";
    
    public static void AddGeminiChatAgents(this IServiceCollection sc, IConfiguration configuration)
    {
        sc.AddNamedSingletons<GeminiChatAgent>(configuration, static (sp, config, name, optional) =>
        {
            if (optional && !config.SectionExists(ConfigurationSection, name))
                return null;
            
            var options = config.MonitorRequiredSection<GeminiChatAgentOptions>(ConfigurationSection, name);
            var logger = sp.GetLogger<GeminiChatAgent>(name);
            return new(options, logger, sp);
        });
    }
}
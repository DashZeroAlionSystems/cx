using CX.Engine.Common;
using CX.Engine.Common.Stores.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CX.Engine.CognitiveServices.ConversationAnalysis;

public static class ConversationAnalyzerDI
{
    public const string ConfigurationSection = "ConversationAnalyzers";
    public const string ConfigurationTableName = "config_conversation_analyzers";
    
    public static void AddConversationAnalyzers(this IServiceCollection sc, IConfiguration configuration)
    {
        sc.AddTypedJsonConfigTable<ConversationAnalyzerOptions>(configuration, ConfigurationSection, ConfigurationTableName);
        
        sc.AddNamedSingletons<ConversationAnalyzer>(configuration, static (sp, config, name, optional) =>
        {
            var section = config.GetSection(ConfigurationSection, name);
            
            if (optional && !section.Exists())
                return null;
            
            section.ThrowIfDoesNotExist($"No configuration section found for {ConfigurationSection} named {name}");
            
            var logger = sp.GetLogger<ConversationAnalyzer>(name);
            var optionsSection = section.GetJsonOptionsMonitor<ConversationAnalyzerOptions>(logger, sp);
            
            return new(name, optionsSection, logger, sp);
        });
    }
}
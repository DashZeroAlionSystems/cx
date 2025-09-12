using CX.Engine.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CX.Engine.TextProcessors;

public static class AzureAITranslatorDI
{
    public const string ConfigurationSection = "AzureAITranslators";
    
    public static void AddAzureAITranslators(this IServiceCollection sc, IConfiguration configuration)
    {
        sc.AddNamedSingletons(configuration, (sp, config, name, optional) =>
        {
            if (optional && !config.SectionExists(ConfigurationSection, name))
                return null;
            
            var options = config.GetRequiredSection(ConfigurationSection).GetRequiredSection<AzureAITranslatorOptions>(name);
            var logger = sp.GetLogger<AzureAITranslator>(name);
            return new AzureAITranslator(options, logger);
        });
    }
}
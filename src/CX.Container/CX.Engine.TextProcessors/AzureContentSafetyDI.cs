using CX.Engine.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CX.Engine.TextProcessors;

public static class AzureContentSafetyDI
{
    public const string ConfigurationSection = "AzureContentSafety";
    
    public static void AddAzureContentSafety(this IServiceCollection sc, IConfiguration configuration)
    {
        sc.AddNamedSingletons(configuration, (sp, config, name, optional) =>
        {
            if (optional && !config.SectionExists(ConfigurationSection, name))
                return null;
            
            var options = config.GetRequiredSection(ConfigurationSection).GetRequiredSection<AzureContentSafetyOptions>(name);
            var logger = sp.GetLogger<AzureContentSafety>(name);
            return new AzureContentSafety(options, logger);
        });
    }
}
using CX.Engine.Assistants;
using CX.Engine.Assistants.ContextAI;
using CX.Engine.Common;
using CX.Engine.Common.Stores.Json;
using CX.Engine.Common.Tracing.Langfuse;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CX.Engine.Assistants.AssessmentBuilder;

public static class AssessmentAssistantDI
{
    public const string ConfigurationSection = "AssessmentAssistants";
    public const string ConfigurationTableName = "config_assessment_assistants";
    
    public static void AddAssessmentBuilderAssistants(this IServiceCollection sc, IConfiguration configuration)
    {
        sc.AddTypedJsonConfigTable<AssessmentAssistantOptions>(configuration, ConfigurationSection, ConfigurationTableName);
        sc.Configure<AssessmentAssistantOptions>(configuration.GetSection(ConfigurationSection));
        sc.AddNamedTransients<AssessmentAssistant>(configuration, static (sp, config, name, optional) =>
        {
            var section = config.GetSection(ConfigurationSection, name);
            
            if (optional && !section.Exists())
                return null;

            section.ThrowIfDoesNotExist($"No configuration found for {nameof(AssessmentAssistant)} named {name.SignleQuoteAndEscape()}");
            
            var logger = sp.GetLogger<AssessmentAssistant>(name);
            var optionsSection = section.GetJsonOptionsMonitor<AssessmentAssistantOptions>(logger, sp);
            
            return new(name, optionsSection, sp, logger, sp.GetRequiredService<LangfuseService>(), sp.GetRequiredService<ContextAIService>());
        });
    }
}
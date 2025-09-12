using CX.Engine.Common;
using CX.Engine.Common.Stores.Json;
using CX.Engine.Common.Tracing.Langfuse;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CX.Engine.Assistants.TextToSchema;

public static class TextToSchemaDI
{
    public const string ConfigurationSection = "TextToSchemaAssistants";
    public const string ConfigurationTableName = "config_text_to_schema_assistants";
    
    public static void AddTextToSchemaAssistants(this IServiceCollection sc, IConfiguration configuration)
    {
        sc.AddTypedJsonConfigTable<TextToSchemaOptions>(configuration, ConfigurationSection, ConfigurationTableName);
        sc.AddSingleton<TextToSchemaOptionsValidator>();
        sc.AddNamedTransients<TextToSchemaAssistant>(configuration, static (sp, config, name, optional) =>
        {
            if (optional && !config.SectionExists(ConfigurationSection, name))
                return default;
            
            var options = config.MonitorRequiredSection(ConfigurationSection, name, config => new TextToSchemaOptionsSetup(config));
            var logger = sp.GetLogger<TextToSchemaAssistant>(name);
            return new(name, options, sp, logger, config, sp.GetRequiredService<LangfuseService>());
        });
    }
}
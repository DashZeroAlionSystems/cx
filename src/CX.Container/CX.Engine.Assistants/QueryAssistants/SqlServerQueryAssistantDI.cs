using CX.Engine.Assistants.ArtifactAssists;
using CX.Engine.Common;
using CX.Engine.Common.Stores.Json;
using CX.Engine.Common.Tracing.Langfuse;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CX.Engine.Assistants.QueryAssistants;

public static class SqlServerQueryAssistantDI
{
    public const string ConfigurationSection = "SqlServerQueryAssistants";
    public const string ConfigurationTableName = "config_sql_server_query_assistants";

    public static void AddSqlServerQueryAssistants(this IServiceCollection sc, IConfiguration configuration)
    {
        sc.AddTypedJsonConfigTable<SqlServerQueryAssistantOptions>(configuration, ConfigurationSection, ConfigurationTableName);
        sc.AddNamedTransients<SqlServerQueryAssistant>(configuration, static (sp, config, name, optional) =>
        {
            if (optional && !config.SectionExists(ConfigurationSection, name))
                return null;
            
            var monitor = config.MonitorRequiredSection<SqlServerQueryAssistantOptions>(ConfigurationSection, name);
            var logger = sp.GetLogger<SqlServerQueryAssistant>(name);
            return new(name, monitor, logger, sp, sp.GetRequiredService<LangfuseService>(), sp.GetRequiredService<Crc32JsonStore>(),
                sp.GetRequiredService<ArtifactAssist>());
        });
    }
}
using CX.Engine.Assistants.ArtifactAssists;
using CX.Engine.Common;
using CX.Engine.Common.Json;
using CX.Engine.Common.Stores.Json;
using CX.Engine.Common.Tracing.Langfuse;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CX.Engine.Assistants.QueryAssistants;

public static class SqlServerReportAssistantDI
{
    public const string ConfigurationSection = "SqlServerReportAssistants";
    public const string ConfigurationTableName = "config_sql_server_report_assistants";

    public static void AddSqlServerReportAssistants(this IServiceCollection sc, IConfiguration configuration)
    {
        sc.AddTypedJsonConfigTable<SqlServerReportAssistantOptions>(configuration, ConfigurationSection, ConfigurationTableName);
        sc.AddNamedTransients<SqlServerReportAssistant>(configuration, static (sp, config, name, optional) =>
        {
            var section = config.GetSection(ConfigurationSection, name);
            
            if (optional && !section.Exists())
                return null;
            
            section.ThrowIfDoesNotExist($"No configuration section found for {ConfigurationSection} named {name}");
            
            var logger = sp.GetLogger<SqlServerReportAssistant>(name);
            var optionsSection = section.GetJsonOptionsMonitor<SqlServerReportAssistantOptions>(logger, sp);
            
            return new(name, optionsSection, logger, sp, sp.GetRequiredService<LangfuseService>(),
                sp.GetRequiredService<ArtifactAssist>());
        });
    }
}
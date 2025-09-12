using CX.Engine.Common.Stores.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CX.Engine.Common.PostgreSQL;

public static class PostgreSQLClientDI
{
    public const string ConfigurationSection = "PostgreSQLClient";
    public const string ConfigurationTableName = "config_postgresqlclients";
    
    public static void AddPostgreSQLClients(this IServiceCollection sc, IConfiguration configuration)
    {
        sc.AddTypedJsonConfigTable<PostgreSQLClientOptions>(configuration, ConfigurationSection, ConfigurationTableName);

        sc.AddNamedSingletons<PostgreSQLClient>(configuration, static (sp, config, name, optional) =>
        {
            if (optional && !config.SectionExists(ConfigurationSection, name))
                return null;
            
            var opts = config.MonitorRequiredSection<PostgreSQLClientOptions>(ConfigurationSection, name);
            var logger = sp.GetLogger<PostgreSQLClient>(name);
            return new(opts, logger, sp);
        });
    }
}
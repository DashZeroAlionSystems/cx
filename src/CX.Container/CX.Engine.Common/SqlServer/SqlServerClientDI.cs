using CX.Engine.Common.SqlServer;
using CX.Engine.Common.Stores.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CX.Engine.Common.PostgreSQL;

public static class SqlServerClientDI
{
    public const string ConfigurationSection = "SqlServerClient";
    public const string ConfigurationTableName = "config_sql_server_clients";
    
    public static void AddSqlServerClients(this IServiceCollection sc, IConfiguration configuration)
    {
        sc.AddTypedJsonConfigTable<SqlServerClientOptions>(configuration, ConfigurationSection, ConfigurationTableName);

        sc.AddNamedSingletons<SqlServerClient>(configuration, static (sp, config, name, optional) =>
        {
            if (optional && !config.SectionExists(ConfigurationSection, name))
                return null;
            
            var opts = config.MonitorRequiredSection<SqlServerClientOptions>(ConfigurationSection, name);
            var logger = sp.GetLogger<SqlServerClient>(name);
            return new(opts, logger, sp);
        });
    }
}
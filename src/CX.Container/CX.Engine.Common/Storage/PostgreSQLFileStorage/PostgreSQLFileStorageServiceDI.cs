using CX.Engine.Common.Stores.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CX.Engine.Common.Storage.PostgreSQLFileStorage;

public static class PostgreSQLStorageServiceDI
{
    private static string ConfigurationSection = "PostgresStorageService";
    private static string ConfigurationTableName = "config_postgres_file_storage";
    
    public static void AddPostgreSQLStorageServices(this IServiceCollection sc, IConfiguration configuration)
    {
        sc.AddTypedJsonConfigTable<PostgreSQLStorageServiceOptions>(configuration, ConfigurationSection, ConfigurationTableName);

        sc.AddNamedSingletons<PostgreSQLStorageService>(configuration, static (sp, config, name, optional) =>
        {
            var section = config.GetSection(ConfigurationSection, name);
            
            if (optional && !section.Exists())
                return null;
            
            section.ThrowIfDoesNotExist($"No configuration section found for {ConfigurationSection} named {name}");
            
            var logger = sp.GetLogger<PostgreSQLStorageService>(name);
            var optionsSection = section.GetJsonOptionsMonitor<PostgreSQLStorageServiceOptions>(logger, sp);
            
            return new(optionsSection, logger, sp, $"{StorageServiceDI.PgStorageEngineName}.{name}");
        });
    }
}
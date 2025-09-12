using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CX.Engine.Common.Stores.Binary.PostgreSQL;

public static class PostgreSQLBinaryStoreDI
{
    public const string ConfigurationSection = "PostgreSQLBinaryStores";
    
    public static void AddPostgreSQLBinaryStores(this IServiceCollection sc, IConfiguration configuration)
    {
        sc.AddNamedSingletons<PostgreSQLBinaryStore>(configuration, static (sp, config, name, optional) => {
            if (optional && !config.SectionExists(ConfigurationSection, name))
                return null;
            
            var options = config.GetRequiredSection(ConfigurationSection).GetRequiredSection<PostgreSQLBinaryStoreOptions>(name);
            var logger = sp.GetLogger<PostgreSQLBinaryStore>(name);
            return new(options, logger, sp);
        });
    }
}
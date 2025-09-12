using CX.Engine.Common.Stores.Json;

namespace CX.Engine.Archives.TableArchives;

public static class PgTableArchiveDI
{
    public const string ConfigurationSection = "PgTableArchives";
    public const string ConfigurationTableName = "config_pg_table_archives";
    
    public static void AddPgTableArchives(this IServiceCollection sc, IConfiguration configuration)
    {
        sc.AddTypedJsonConfigTable<PgTableArchive>(configuration, ConfigurationSection, ConfigurationTableName);
        
        sc.AddNamedSingletons<PgTableArchive>(configuration, static (sp, config, name, optional) =>
        {
            var section = config.GetSection(ConfigurationSection, name);
            
            if (optional && !section.Exists())
                return null;

            section.ThrowIfDoesNotExist($"No configuration found for {nameof(PgTableArchive)} named {name.SignleQuoteAndEscape()}");
            
            var logger = sp.GetLogger<PgTableArchive>(name);
            var monitor = section.GetJsonOptionsMonitor<PgTableArchiveOptions>(logger, sp);
            
            return new(name, monitor, logger, sp);
        });
    }
}
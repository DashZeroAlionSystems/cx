using CX.Engine.Common.Embeddings;
using CX.Engine.Common.Stores.Json;

namespace CX.Engine.Archives.PgVector;

public static class PgVectorArchiveDI
{
    public const string ConfigurationSection = "PgVectorArchives";
    public const string ConfigurationTableName = "config_pgvector_archives";

    public static void AddPgVectorArchives(this IServiceCollection sc, IConfiguration configuration)
    {
        sc.AddTypedJsonConfigTable<PgVectorArchiveOptions>(configuration, ConfigurationSection, ConfigurationTableName);
 
        sc.AddNamedSingletons<PgVectorChunkArchive>(configuration,
            (sp, config, name, optional) =>
            {
                if (optional && !config.SectionExists(ConfigurationSection, name))
                    return null;
                
                var opts = config.MonitorRequiredSection<PgVectorArchiveOptions>(ConfigurationSection, name);
                var logger = sp.GetLogger<PgVectorChunkArchive>(name);
                return new(opts, logger, sp, sp.GetRequiredService<EmbeddingCache>());
            });
    }
}
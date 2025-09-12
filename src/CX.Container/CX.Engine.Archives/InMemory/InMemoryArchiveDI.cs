using CX.Engine.Common.Embeddings;

namespace CX.Engine.Archives.InMemory;

public static class InMemoryArchiveDI
{
    public const string ConfigurationSection = "InMemoryArchives";
    
    public static void AddInMemoryArchives(this IServiceCollection sc, IConfiguration configuration)
    {
        sc.AddNamedSingletons<InMemoryChunkArchive>(configuration,
            (sp, config, name, optional) =>
            {
                var opts = config.GetSection<InMemoryArchiveOptions>(ConfigurationSection, name, optional);

                if (opts == null)
                    return default;
                
                opts.Name = name;
                return new(sp.GetRequiredService<EmbeddingCache>(), opts);
            });
    }
}
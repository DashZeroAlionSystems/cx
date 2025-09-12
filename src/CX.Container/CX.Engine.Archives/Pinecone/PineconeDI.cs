using CX.Engine.Common.Embeddings;
using CX.Engine.Common.Stores.Json;

namespace CX.Engine.Archives.Pinecone;

public static class PineconeDI
{
    public const string ConfigurationSection = "Pinecone";
    public const string ConfigurationTableName = "config_pinecones";
    public const string ConfigurationSectionNamespaces = "PineconeNamespaces";
    public const string ConfigurationTableNameNamespaces = "config_pinecone_namespaces";
    
    public static void AddPinecone1Archives(this IServiceCollection sc, IConfiguration configuration)
    {
        sc.AddTypedJsonConfigTable<PineconeOptions>(configuration, ConfigurationSection, ConfigurationTableName);
        sc.AddTypedJsonConfigTable<PineconeNamespaceOptions>(configuration, ConfigurationSectionNamespaces, ConfigurationTableNameNamespaces);

        sc.AddNamedSingletons<PineconeChunkArchive>(configuration,
            (sp, config, name, optional) =>
            {
                if (optional && !config.SectionExists(ConfigurationSection, name))
                    return null;
                
                var opts = config.MonitorRequiredSection<PineconeOptions>(ConfigurationSection, name);
                var logger = sp.GetLogger<PineconeChunkArchive>(name);
                return new(name, opts, sp.GetRequiredService<EmbeddingCache>(), sp, logger);
            });
        
        sc.AddNamedSingletons<PineconeReadOnlyChunkArchive>(configuration,
            (sp, config, name, optional) =>
            {
                if (optional && !config.SectionExists(ConfigurationSection, name))
                    return null;
                
                var opts = config.MonitorRequiredSection<PineconeOptions>(ConfigurationSection, name);
                var logger = sp.GetLogger<PineconeReadOnlyChunkArchive>(name);
                return new(name, opts, sp.GetRequiredService<EmbeddingCache>(), sp, logger);
            });

        sc.AddNamedSingletons<PineconeNamespace>(configuration,
            (sp, config, name, optional) =>
            {
                if (optional && !config.SectionExists(ConfigurationSectionNamespaces, name))
                    return null;
                
                var opts = config.MonitorRequiredSection<PineconeNamespaceOptions>(ConfigurationSectionNamespaces, name);
                var logger = sp.GetLogger<PineconeNamespace>(name);
                return new(opts, sp, logger);
            });
    }
}
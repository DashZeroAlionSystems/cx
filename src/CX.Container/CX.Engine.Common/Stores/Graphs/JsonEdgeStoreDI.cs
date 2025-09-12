using CX.Engine.Common.Stores.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CX.Engine.Common.Stores.Graphs;

public static class JsonEdgeStoreDI
{
    public const string ConfigurationSection = "JsonEdgeStores";
    public const string ConfigurationTableName = "config_json_edge_stores";
    
    public static void AddJsonEdgeStores(this IServiceCollection sc, IConfiguration configuration)
    {
        sc.AddTypedJsonConfigTable<JsonEdgeStoreOptions>(configuration, ConfigurationSection, ConfigurationTableName);
        sc.Configure<JsonEdgeStoreOptions>(configuration.GetSection(ConfigurationSection));
        sc.AddNamedSingletons<JsonEdgeStore>(configuration, (sp, config, name, optional) =>
        {
            if (optional && !config.SectionExists(ConfigurationSection, name))
                return null;
            
            var monitor = configuration.MonitorRequiredSection<JsonEdgeStoreOptions>(ConfigurationSection, name);
            var logger = sp.GetLogger<JsonEdgeStore>(name);
            return new(name, monitor, logger, sp);
        });
        
    }
}
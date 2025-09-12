using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CX.Engine.Common.Stores.Json;

public static class JsonObjectStoreDI
{
    public const string ConfigurationSection = "JsonObjectStores";
    public const string ConfigurationTableName = "config_json_object_stores";
    
    public static void AddJsonObjectStores(this IServiceCollection sc, IConfiguration configuration)
    {
        sc.AddTypedJsonConfigTable<JsonObjectStoreOptions>(configuration, ConfigurationSection, ConfigurationTableName);
        sc.Configure<JsonObjectStoreOptions>(configuration.GetSection(ConfigurationSection));
        sc.AddNamedSingletons<JsonObjectStore>(configuration, (sp, config, name, optional) =>
        {
            if (optional && !config.SectionExists(ConfigurationSection, name))
                return null;
            
            var monitor = configuration.MonitorRequiredSection<JsonObjectStoreOptions>(ConfigurationSection, name);
            var logger = sp.GetLogger<JsonObjectStore>(name);
            return new(name, monitor, logger, sp);
        });
    }
}
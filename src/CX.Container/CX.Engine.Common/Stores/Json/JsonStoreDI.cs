using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CX.Engine.Common.Stores.Json;

public static class JsonStoreDI
{
    public const string ConfigurationSection = "JsonStores";
    
    public static void AddJsonStores(this IServiceCollection sc, IConfiguration configuration)
    {
        sc.AddSingleton<Crc32JsonStore>();
        sc.AddNamedSingletons<IJsonStore>(configuration, static (sp, config, name, optional) => {
            if (optional && !config.SectionExists(ConfigurationSection, name))
                return null;
            
            var options = config.GetRequiredSection(ConfigurationSection).GetRequiredSection<JsonStoreOptions>(name);
            var logger = sp.GetLogger<JsonStore>(name);
            return new JsonStore(options, logger, sp);
        });
    }
}
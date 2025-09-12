using CX.Engine.Common.Stores.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CX.Engine.Common.Storage.BlobStorage;

public static class BlobStorageServiceDI
{
    private static string ConfigurationSection = "BlobStorageService";
    private static string ConfigurationTableName = "config_blob_file_storage";
    
    public static void AddBlobStorageServices(this IServiceCollection sc, IConfiguration configuration)
    {
        sc.AddTypedJsonConfigTable<BlobStorageServiceOptions>(configuration, ConfigurationSection, ConfigurationTableName);

        sc.AddNamedSingletons<BlobStorageService>(configuration, static (sp, config, name, optional) =>
        {
            var section = config.GetSection(ConfigurationSection, name);
            
            if (optional && !section.Exists())
                return null;
            
            section.ThrowIfDoesNotExist($"No configuration section found for {ConfigurationSection} named {name}");
            
            var logger = sp.GetLogger<BlobStorageService>(name);
            var optionsSection = section.GetJsonOptionsMonitor<BlobStorageServiceOptions>(logger, sp);
            
            return new(optionsSection, logger, sp, $"{StorageServiceDI.BlobStorageEngineName}.{name}");
        });
    }
}
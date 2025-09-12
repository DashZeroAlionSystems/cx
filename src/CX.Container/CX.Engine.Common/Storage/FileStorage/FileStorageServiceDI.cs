using CX.Engine.Common.Stores.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CX.Engine.Common.Storage.FileStorage;

public static class FileStorageServiceDI
{
    private static string ConfigurationSection = "StorageService";
    private static string ConfigurationTableName = "config_file_storage";
    
    public static void AddFileStorageService(this IServiceCollection sc, IConfiguration configuration)
    {
        sc.AddTypedJsonConfigTable<FileStorageServiceOptions>(configuration, ConfigurationSection, ConfigurationTableName);

        sc.AddNamedSingletons<FileStorageService>(configuration, static (sp, config, name, optional) =>
        {
            var section = config.GetSection(ConfigurationSection, name);
            
            if (optional && !section.Exists())
                return null;
            
            section.ThrowIfDoesNotExist($"No configuration section found for {ConfigurationSection} named {name}");
            
            var logger = sp.GetLogger<FileStorageService>(name);
            var optionsSection = section.GetJsonOptionsMonitor<FileStorageServiceOptions>(logger, sp);
            
            return new(optionsSection, logger, sp, $"{StorageServiceDI.StorageEngineName}.{name}");
        });
    }
}
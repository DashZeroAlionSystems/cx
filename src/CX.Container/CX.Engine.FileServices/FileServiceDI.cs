using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CX.Engine.FileServices;

public static class FileServiceDI
{
    public const string ConfigurationSection = "FileService";
    
    public static void AddFileService(this IServiceCollection sc, IConfiguration configuration)
    {
        sc.Configure<FileServiceOptions>(configuration.GetSection(ConfigurationSection));
        sc.AddSingleton<FileService>();
    }
}
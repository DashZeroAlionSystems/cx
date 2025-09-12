using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CX.Engine.Importers;

public static class DiskImporterDI
{
    public static void AddDiskImporter(this IServiceCollection sc, IConfiguration configuration)
    {
        sc.Configure<DiskImporterOptions>(configuration.GetSection("DiskImporter"));
        sc.AddSingleton<DiskImporter>();
    }
}
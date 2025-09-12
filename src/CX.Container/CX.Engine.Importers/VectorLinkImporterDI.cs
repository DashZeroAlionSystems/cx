using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CX.Engine.Importing;

public static class VectorLinkImporterDI
{
    public const string ConfigurationSection = "VectorLinkImporter";
    
    public static void AddVectorLinkImporter(this IServiceCollection sc, IConfiguration configuration)
    {
        sc.Configure<VectorLinkImporterOptions>(configuration.GetSection(ConfigurationSection));
        sc.AddSingleton<VectorLinkImporter>();
    }
}
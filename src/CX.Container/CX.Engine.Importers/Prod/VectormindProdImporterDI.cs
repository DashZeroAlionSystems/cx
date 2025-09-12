using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CX.Engine.Importing.Prod;

public static class VectormindProdImporterDI
{
    public static void AddVectormindProdImporter(this IServiceCollection sc, IConfiguration configuration)
    {
        sc.AddProdRepo(configuration);
        sc.AddProdS3Helpers(configuration);
        sc.Configure<VectormindProdImporterOptions>(configuration.GetSection("VectormindProdImporter"));
        sc.AddSingleton<VectormindProdImporter>();
    }
}
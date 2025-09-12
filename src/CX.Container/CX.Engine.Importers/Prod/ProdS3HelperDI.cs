using CX.Engine.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CX.Engine.Importing.Prod;

public static class ProdS3HelperDI
{
    public const string ConfigurationSection = "ProdS3Helpers";
    
    public static void AddProdS3Helpers(this IServiceCollection sc, IConfiguration configuration)
    {
        sc.AddNamedSingletons<ProdS3Helper>(configuration, static (_, configuration, name, optional) =>
        {
            if (optional && !configuration.SectionExists(ConfigurationSection, name))
                return null;
            
            var options = configuration.GetRequiredSection<ProdS3HelperOptions>(ConfigurationSection, name);
            return new(options);
        });
    }
}
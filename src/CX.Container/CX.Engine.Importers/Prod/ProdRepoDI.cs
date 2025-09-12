using CX.Engine.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CX.Engine.Importing.Prod;

public static class ProdRepoDI
{
    public const string ConfigurationSection = "ProdRepos";
    
    public static void AddProdRepo(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddNamedSingletons<ProdRepo>(configuration, static (sp, configuration, name, optional) =>
        {
            if (optional && !configuration.SectionExists(ConfigurationSection, name))
                return null;
            
            var options = configuration.GetRequiredSection<ProdRepoOptions>(ConfigurationSection, name);
            return new(options, sp);
        });
    }
}
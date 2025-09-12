using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CX.Engine.Common.Tracing.Langfuse;

public static class LangfuseDI
{
    public const string ConfigurationSection = "Langfuse";
    
    public static void AddLangfuse(this IServiceCollection sc, IConfiguration configuration)
    {
        sc.Configure<LangfuseOptions>(configuration.GetSection(ConfigurationSection));
        sc.AddSingleton<LangfuseService>();
        sc.AddSingleton<IHostedService>(sp => sp.GetRequiredService<LangfuseService>());
    }
}
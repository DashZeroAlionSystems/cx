using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CX.Engine.DocExtractors.Text;

public static class Gpt4VisionExtractorDI
{
    public const string ConfigurationSection = "Gpt4VisionExtractor";
    
    public static void AddGpt4VisionExtractor(this IServiceCollection sc, IConfiguration configuration)
    {
        sc.Configure<Gpt4VisionExtractorOptions>(configuration.GetSection(ConfigurationSection));
        sc.AddSingleton<Gpt4VisionExtractor>();
    }    
}
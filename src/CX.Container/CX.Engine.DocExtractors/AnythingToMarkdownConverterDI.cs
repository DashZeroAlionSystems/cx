using Aela.Server.Converters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CX.Engine.DocExtractors.Text;

public static class MarkdownConverterDI
{
    public const string ConfigurationSection = "MarkItDownConverter";
    
    public static void AddMarkdownConverter(this IServiceCollection sc, IConfiguration configuration)
    {
        // Configure options
        sc.Configure<AnythingToMarkdownOptions>(configuration.GetSection(ConfigurationSection));
        
        // Register the extractor
        sc.AddScoped<AnythingToMarkdownExtractor>();
    }
}
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CX.Engine.DocExtractors.Text;

public static class PDFPlumberDI
{
    public const string ConfigurationSection = "PDFPlumber";
    
    public static void AddPdfPlumber(this IServiceCollection sc, IConfiguration configuration)
    {
        sc.Configure<PDFPlumberOptions>(configuration.GetSection(ConfigurationSection));
        sc.AddSingleton<PDFPlumber>();
    }
}
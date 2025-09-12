using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CX.Engine.DocExtractors.Images;

public static class PDFToJpgDI
{
    public const string ConfigurationSection = "PDFToJpg";
    
    public static void AddPDFToJpg(this IServiceCollection sc, IConfiguration configuration)
    {
        sc.Configure<PDFToJpgOptions>(configuration.GetSection(ConfigurationSection));
        sc.AddSingleton<PDFToJpg>();
    }
    
}
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CX.Engine.DocExtractors;

public static class DocXToPDFDI
{
    public const string ConfigurationSection = "DocXToPDF";
    
    public static void AddDocXoPdf(this IServiceCollection sc, IConfiguration configuration)
    {
        sc.Configure<DocXToPDFOptions>(configuration.GetSection(ConfigurationSection));
        sc.AddSingleton<DocXToPDF>();
    }
}
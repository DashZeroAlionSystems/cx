using CX.Engine.DocExtractors.Images;
using CX.Engine.DocExtractors.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CX.Engine.DocExtractors;

public static class DocExtractorsDI
{
    
    public static void AddMSDocAnalyzer(this IServiceCollection sc, IConfiguration configuration)
    {
        sc.Configure<MSDocAnalyzerOptions>(configuration.GetSection("MSDocAnalyzer"));
        sc.AddSingleton<MSDocAnalyzer>();
    }
    
    public static void AddDocumentExtractors(this IServiceCollection sc, IConfiguration configuration)
    {
        sc.AddPdfPlumber(configuration);
        sc.AddPythonDocX(configuration);
        sc.AddMSDocAnalyzer(configuration);
        sc.AddGpt4VisionExtractor(configuration);
        sc.AddSingleton<DocTextExtractionRouter>();
        
        sc.AddDocXoPdf(configuration);
        sc.AddPDFToJpg(configuration);
        sc.AddSingleton<DocImageExtraction>();
    }
}
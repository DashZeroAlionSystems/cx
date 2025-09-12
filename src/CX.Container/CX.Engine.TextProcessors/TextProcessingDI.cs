using CX.Engine.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CX.Engine.TextProcessors;

public static class TextProcessingDI
{
    public const string AzureContentSafety = "azure-content-safety";
    
    public static void AddTextProcessors(this IServiceCollection sc, IConfiguration configuration)
    {
        sc.AddAzureAITranslators(configuration);
        sc.AddAzureContentSafety(configuration);
        
        sc.AddNamedSingletons<ITextProcessor>(configuration, (sp, _, name, optional) => {
            var (engine, subName) = name.SplitAtFirst(".");
            
            switch (engine)
            {
                case "azure-ai-translator":
                    return sp.GetNamedService<AzureAITranslator>(subName, optional);
                case AzureContentSafety:
                    return sp.GetNamedService<AzureContentSafety>(subName, optional);
                default:
                    throw new InvalidOperationException($"Unknown text processor engine: {engine}");
            }
        });
    }
    
    public static async Task<string> ProcessAsync(string input, IServiceProvider sp, params string[] processors)
    {
        var text = input;
        
        if (processors != null)
        {
            foreach (var name in processors)
            {
                if (string.IsNullOrWhiteSpace(name))
                    continue;
                        
                var processor = sp.GetRequiredNamedService<ITextProcessor>(name);
                text = await processor.ProcessAsync(text) ?? text;
            }
        }
        
        return text;
    }
}
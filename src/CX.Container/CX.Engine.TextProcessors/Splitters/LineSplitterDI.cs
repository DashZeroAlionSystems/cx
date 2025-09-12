using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CX.Engine.TextProcessors.Splitters;

public static class LineSplitterDI
{
    public const string ConfigurationSection = "LineSplitter";
    
    public static void AddLineSplitter(this IServiceCollection sc, IConfiguration configuration)
    {
        sc.Configure<LineSplitterOptions>(configuration.GetSection(ConfigurationSection));
        sc.AddTransient<LineSplitter>();
    }
}
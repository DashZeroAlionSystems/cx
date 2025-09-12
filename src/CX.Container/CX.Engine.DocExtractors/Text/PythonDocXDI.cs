using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CX.Engine.DocExtractors.Text;

public static class PythonDocXDI
{
    public const string ConfigurationSection = "PythonDocX";
    
    public static void AddPythonDocX(this IServiceCollection sc, IConfiguration configuration)
    {
        sc.Configure<PythonDocXOptions>(configuration.GetSection(ConfigurationSection));
        sc.AddSingleton<PythonDocX>();
    }
}
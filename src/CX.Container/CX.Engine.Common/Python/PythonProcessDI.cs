using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CX.Engine.Common.Python;

public static class PythonProcessDI
{
    public const string ConfigurationSection = "PythonProcess";
    
    public static void AddPythonProcesses(this IServiceCollection sc, IConfiguration configuration)
    {
        sc.AddNamedSingletons<PythonProcess>(configuration, static (_, config, name, optional) => {
            if (optional && !config.SectionExists(ConfigurationSection, name))
                return null;
            
            var options = config.GetRequiredSection(ConfigurationSection).GetRequiredSection<PythonProcessOptions>(name);
            return new(options);    
        });
    }
}
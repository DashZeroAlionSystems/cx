using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CX.Engine.Assistants.ContextAI;

public static class ContextAIDI
{
    public const string ConfigurationSection = "ContextAI";
    
    public static void AddContextAI(this IServiceCollection sc, IConfiguration configuration)
    {
        sc.Configure<ContextAIOptions>(configuration.GetSection(ConfigurationSection));
        sc.AddSingleton<ContextAIService>();
        sc.AddSingleton<IHostedService>(sp => sp.GetRequiredService<ContextAIService>());
    }
}
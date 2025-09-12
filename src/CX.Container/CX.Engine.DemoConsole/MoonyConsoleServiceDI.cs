using CX.Engine.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CX.Engine.DemoConsole;

public static class MoonyConsoleServiceDI
{
    public const string ConfigurationSection = "MoonyConsoleService";
    public const string LuaCoreLibraryMoonyConsoleService = "MoonyConsoleService";
    
    public static void AddMoonyConsoleService(this IServiceCollection sc, IConfiguration configuration)
    {
        sc.Configure<MoonyConsoleServiceOptions>(configuration.GetSection(ConfigurationSection));
        sc.AddKeyedSingleton<ILuaCoreLibrary>(LuaCoreLibraryMoonyConsoleService, (sp, _) => sp.GetService<MoonyConsoleService>());
        sc.AddSingleton<MoonyConsoleService>();
        sc.AddSingleton<IHostedService>(sp => sp.GetRequiredService<MoonyConsoleService>());
    }
}